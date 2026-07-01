using desktop_server_app.Config;
using desktop_server_app.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace desktop_server_app.Components
{
    /// <summary>
    /// Task 3 — API caller / file uploader.
    /// Consumes <see cref="Mp3MetadataBatch"/> from <c>playlist-extract-queue</c>,
    /// uploads each .mp3 file to the API, collects the server file path,
    /// then POSTs all metadata (including server path) to the API.
    /// On full success publishes an <see cref="UploadSuccessMessage"/> to
    /// <c>playlist-upload-queue</c> so Task 4 can clean up.
    /// </summary>
    internal class CallApiProgram
    {
        private readonly string _metadataEndpoint;
        private readonly string _uploadEndpoint;
        private readonly string _blacklistEndpoint;
        private readonly HttpClient _http;

        private static readonly string _durationLimitPath = Path.Combine(
            Environment.CurrentDirectory, "Config", "json", "limit_duree_second.json");

        private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

        public CallApiProgram()
        {
            var api = AppConfig.Root.GetSection("Api");

            _metadataEndpoint = api["MetadataEndpoint"]
                ?? throw new InvalidOperationException("Missing 'Api:MetadataEndpoint'.");
            _uploadEndpoint = api["UploadEndpoint"]
                ?? throw new InvalidOperationException("Missing 'Api:UploadEndpoint'.");

            // We construct the blacklist endpoint assuming standard REST structure
            _blacklistEndpoint = "http://localhost:5001/api/blacklist";

            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(
                    int.TryParse(api["TimeoutSeconds"], out var t) ? t : 30)
            };

            var token = api["BearerToken"];
            if (!string.IsNullOrWhiteSpace(token))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            AppLogger.Log("Task3",
                $"Started — consuming '{RabbitMqConnection.ExtractQueue}'.");

            using var connection = await RabbitMqConnection.Factory
                .CreateConnectionAsync(cancellationToken);
            using var channel = await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

            await RabbitMqConnection.DeclareQueueAsync(
                channel, RabbitMqConnection.ExtractQueue, cancellationToken);
            await RabbitMqConnection.DeclareQueueAsync(
                channel, RabbitMqConnection.UploadQueue, cancellationToken);

            await channel.BasicQosAsync(
                prefetchSize: 0, prefetchCount: 1, global: false,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var batch = JsonSerializer.Deserialize<Mp3MetadataBatch>(json);

                    if (batch is null || batch.Tracks.Count == 0)
                    {
                        AppLogger.Log("Task3", "Received empty metadata batch — discarding.");
                        await channel.BasicNackAsync(
                            ea.DeliveryTag, false, false, cancellationToken);
                        return;
                    }

                    AppLogger.Log("Task3",
                        $"Received metadata batch — {batch.Tracks.Count} track(s) " +
                        $"from '{batch.SourceDir}'.");

                    // ── Step A : Upload all files, collect server file paths ──
                    var serverPaths = new Dictionary<string, string>(); // key = original FilePath
                    bool uploadFailed = false;

                    // Fetch current blacklist
                    var blacklist = await FetchBlacklistAsync(cancellationToken);
                    int durationLimit = ReadDurationLimitSeconds();
                    var validTracks = new List<Mp3Metadata>();
                    var rejectedFileNames = new List<string>(); // fichiers rejetés (blacklist/durée)

                    foreach (var track in batch.Tracks)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Check blacklist
                        bool isBlacklisted = false;
                        foreach (var rule in blacklist)
                        {
                            if (rule.RuleType == "Artist" && track.Artists.Any(a => a.Equals(rule.Value, StringComparison.OrdinalIgnoreCase)))
                                isBlacklisted = true;
                            if (rule.RuleType == "Genre" && track.Genres.Any(g => g.Equals(rule.Value, StringComparison.OrdinalIgnoreCase)))
                                isBlacklisted = true;
                            if (rule.RuleType == "Title" && track.Title.Equals(rule.Value, StringComparison.OrdinalIgnoreCase))
                                isBlacklisted = true;
                        }

                        if (isBlacklisted)
                        {
                            AppLogger.Log("Task3", $"Track '{track.FileName}' is blacklisted. Rejecting (file kept in scan folder).");
                            rejectedFileNames.Add(track.FileName);
                            continue;
                        }

                        // Check duration limit
                        if (durationLimit > 0 && track.DurationSeconds > durationLimit)
                        {
                            AppLogger.Log("Task3",
                                $"Track '{track.FileName}' exceeds duration limit " +
                                $"({track.DurationSeconds}s > {durationLimit}s). Rejecting (file kept in scan folder).");
                            rejectedFileNames.Add(track.FileName);
                            continue;
                        }

                        if (!File.Exists(track.FilePath))
                        {
                            AppLogger.Log("Task3", $"File not found, skipping: {track.FilePath}");
                            continue;
                        }

                        var serverPath = await UploadOneFileAndGetPathAsync(track, cancellationToken);
                        if (string.IsNullOrEmpty(serverPath))
                        {
                            AppLogger.Log("Task3", $"Upload failed for '{track.FileName}' — aborting batch.");
                            uploadFailed = true;
                            break;
                        }

                        serverPaths[track.FilePath] = serverPath;
                        validTracks.Add(track);
                        AppLogger.Log("Task3", $"Uploaded '{track.FileName}' → server path: {serverPath}");
                    }

                    if (uploadFailed)
                    {
                        AppLogger.Log("Task3", "One or more uploads failed — requeueing batch for retry.");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                        return;
                    }

                    // ── Step B : Build metadata batch with server file paths ──
                    if (validTracks.Count == 0)
                    {
                        AppLogger.Log("Task3", "All tracks in batch were blacklisted or invalid.");
                    }
                    else
                    {
                        var metadataBatch = new MetadataBatchRequest
                        {
                            SourceDir = batch.SourceDir,
                            Tracks = validTracks.Select(track => new MetadataUploadDto
                        {
                            FileName = track.FileName,
                            FilePath = serverPaths[track.FilePath],   // server-side path
                            FileSize = track.FileSize,
                            Title = track.Title,
                            Artists = track.Artists,
                            Language = track.Language,
                            Album = track.Album,
                            AlbumArtist = track.AlbumArtist,
                            Year = track.Year,
                            Track = track.Track,
                            Genres = track.Genres,
                            Comment = track.Comment,
                            DurationSeconds = track.DurationSeconds,
                            BitRate = track.BitRate,
                            SampleRate = track.SampleRate,
                            Channels = track.Channels,
                            CreatedAt = track.CreatedAt,
                            ExtractedAt = track.ExtractedAt
                        }).ToList()
                    };

                        // ── Step C : POST metadata batch to API ──
                        bool metaOk = await PostMetadataBatchAsync(metadataBatch, cancellationToken);
                        if (!metaOk)
                        {
                            AppLogger.Log("Task3", "Metadata batch POST failed — requeueing for retry.");
                            await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                            return;
                        }
                    }

                    // ── Step D : Notify Task 4 (cleanup) ──
                    var success = new UploadSuccessMessage
                    {
                        UploadedAt = DateTime.UtcNow,
                        SourceDir = batch.SourceDir,
                        TrackCount = batch.Tracks.Count,
                        RejectedFileNames = rejectedFileNames
                    };

                    var outBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(success));
                    var props = new BasicProperties { Persistent = true };

                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: RabbitMqConnection.UploadQueue,
                        mandatory: false,
                        basicProperties: props,
                        body: outBody,
                        cancellationToken: cancellationToken);

                    AppLogger.Log("Task3", $"Success message published → '{RabbitMqConnection.UploadQueue}'.");
                    await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                }
                catch (JsonException ex)
                {
                    AppLogger.Log("Task3", $"Bad message format: {ex.Message}");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
                }
                catch (Exception ex)
                {
                    AppLogger.Log("Task3", $"Processing error: {ex.Message}");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                }
            };

            await channel.BasicConsumeAsync(
                queue: RabbitMqConnection.ExtractQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            AppLogger.Log("Task3", "Waiting for metadata batches. Press Ctrl+C to stop.");
            await Task.Delay(Timeout.Infinite, cancellationToken);

            AppLogger.Log("Task3", "Stopped.");
        }

        // ── HTTP helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Uploads a single MP3 file to the API and returns the server-side file path.
        /// </summary>
        private async Task<string?> UploadOneFileAndGetPathAsync(Mp3Metadata track, CancellationToken ct)
        {
            AppLogger.Log("Task3", $"Uploading '{track.FileName}' → {_uploadEndpoint}");
            try
            {
                await using var fs = new FileStream(
                    track.FilePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, bufferSize: 81920, useAsync: true);

                using var content = new MultipartFormDataContent();

                var streamContent = new StreamContent(fs);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                content.Add(streamContent, "file", track.FileName);

                content.Add(new StringContent(track.Title), "title");
                content.Add(new StringContent(string.Join(", ", track.Artists)), "artist");
                content.Add(new StringContent(track.Album), "album");
                content.Add(new StringContent(track.FileName), "fileName");

                using var response = await _http.PostAsync(_uploadEndpoint, content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    AppLogger.Log("Task3", $"Upload failed: HTTP {(int)response.StatusCode} - {body}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<UploadResult>(json);
                return result?.filePath;
            }
            catch (Exception ex)
            {
                AppLogger.Log("Task3", $"Upload exception for '{track.FileName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sends the complete metadata batch (with server file paths) to the API.
        /// </summary>
        private async Task<bool> PostMetadataBatchAsync(MetadataBatchRequest batch, CancellationToken ct)
        {
            AppLogger.Log("Task3",
                $"POST metadata batch ({batch.Tracks.Count} track(s)) → {_metadataEndpoint}");
            try
            {
                using var response = await _http.PostAsJsonAsync(
                    _metadataEndpoint, batch, _jsonOpts, ct);

                if (response.IsSuccessStatusCode)
                {
                    AppLogger.Log("Task3", "Metadata batch accepted.");
                    return true;
                }

                var body = await response.Content.ReadAsStringAsync(ct);
                AppLogger.Log("Task3", $"Metadata batch rejected: HTTP {(int)response.StatusCode} - {body}");
                return false;
            }
            catch (Exception ex)
            {
                AppLogger.Log("Task3", $"Metadata batch exception: {ex.Message}");
                return false;
            }
        }

        private async Task<List<BlacklistRuleDto>> FetchBlacklistAsync(CancellationToken ct)
        {
            try
            {
                var response = await _http.GetAsync(_blacklistEndpoint, ct);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(ct);
                    return JsonSerializer.Deserialize<List<BlacklistRuleDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("Task3", $"Failed to fetch blacklist: {ex.Message}");
            }
            return new List<BlacklistRuleDto>();
        }

        /// <summary>
        /// Reads the duration limit (in seconds) from the local JSON config.
        /// Returns 0 if the file is missing or invalid (meaning no limit).
        /// </summary>
        private static int ReadDurationLimitSeconds()
        {
            try
            {
                if (!File.Exists(_durationLimitPath)) return 0;
                var text = File.ReadAllText(_durationLimitPath).Trim();
                if (int.TryParse(text, out int seconds) && seconds > 0)
                {
                    AppLogger.Log("Task3", $"Duration limit loaded: {seconds}s.");
                    return seconds;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("Task3", $"Failed to read duration limit: {ex.Message}");
            }
            return 0;
        }

        public class BlacklistRuleDto
        {
            public string RuleType { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
    }
}