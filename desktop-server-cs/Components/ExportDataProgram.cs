using desktop_server_app.Config;
using desktop_server_app.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TagLib;

namespace desktop_server_app.Components
{
    /// <summary>
    /// Task 2 — Metadata extractor.
    /// Consumes <see cref="Mp3ScanBatch"/> from <c>playlist-scan-queue</c>,
    /// reads ID3 tags via TagLib for every file, then publishes an
    /// <see cref="Mp3MetadataBatch"/> to <c>playlist-extract-queue</c>.
    /// Does NOT call any other task directly.
    /// </summary>
    internal class ExportDataProgram
    {
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            AppLogger.Log("Task2", $"Started — consuming '{RabbitMqConnection.ScanQueue}'.");

            using var connection = await RabbitMqConnection.Factory
                .CreateConnectionAsync(cancellationToken);
            using var channel = await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

            // Declare both queues this task touches
            await RabbitMqConnection.DeclareQueueAsync(
                channel, RabbitMqConnection.ScanQueue, cancellationToken);
            await RabbitMqConnection.DeclareQueueAsync(
                channel, RabbitMqConnection.ExtractQueue, cancellationToken);

            // Process one batch at a time
            await channel.BasicQosAsync(
                prefetchSize: 0, prefetchCount: 1, global: false,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    // ── Deserialize incoming scan batch ──────────────────────────
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var batch = JsonSerializer.Deserialize<Mp3ScanBatch>(json);

                    if (batch is null || batch.Files.Count == 0)
                    {
                        AppLogger.Log("Task2", "Received empty scan batch — discarding.");
                        await channel.BasicNackAsync(
                            ea.DeliveryTag, multiple: false, requeue: false,
                            cancellationToken: cancellationToken);
                        return;
                    }

                    AppLogger.Log("Task2",
                        $"Received scan batch — {batch.Files.Count} file(s) " +
                        $"from '{batch.SourceDir}' scanned at {batch.ScannedAt:u}.");

                    // ── Extract metadata for every file ──────────────────────────
                    var tracks = ExtractAll(batch, cancellationToken);

                    AppLogger.Log("Task2",
                        $"Extraction complete — {tracks.Count} record(s) ready.");

                    // ── Publish metadata batch → playlist-extract-queue ──────────
                    var metaBatch = new Mp3MetadataBatch
                    {
                        ExtractedAt = DateTime.UtcNow,
                        SourceDir = batch.SourceDir,
                        Tracks = tracks
                    };

                    var outBody = Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(metaBatch));
                    var props = new BasicProperties { Persistent = true };

                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: RabbitMqConnection.ExtractQueue,
                        mandatory: false,
                        basicProperties: props,
                        body: outBody,
                        cancellationToken: cancellationToken);

                    AppLogger.Log("Task2",
                        $"Published metadata batch → '{RabbitMqConnection.ExtractQueue}'.");

                    await channel.BasicAckAsync(
                        ea.DeliveryTag, multiple: false,
                        cancellationToken: cancellationToken);
                }
                catch (JsonException ex)
                {
                    AppLogger.Log("Task2", $"Bad message format: {ex.Message}");
                    await channel.BasicNackAsync(
                        ea.DeliveryTag, multiple: false, requeue: false,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    AppLogger.Log("Task2", $"Processing error: {ex.Message}");
                    await channel.BasicNackAsync(
                        ea.DeliveryTag, multiple: false, requeue: true,
                        cancellationToken: cancellationToken);
                }
            };

            await channel.BasicConsumeAsync(
                queue: RabbitMqConnection.ScanQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            AppLogger.Log("Task2", "Waiting for scan batches. Press Ctrl+C to stop.");
            await Task.Delay(Timeout.Infinite, cancellationToken);

            AppLogger.Log("Task2", "Stopped.");
        }

        // ── Metadata helpers ──────────────────────────────────────────────────────

        private static List<Mp3Metadata> ExtractAll(
            Mp3ScanBatch batch,
            CancellationToken ct)
        {
            var results = new List<Mp3Metadata>(batch.Files.Count);

            foreach (var entry in batch.Files)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    results.Add(ExtractOne(entry));
                }
                catch (Exception ex)
                {
                    AppLogger.Log("Task2",
                        $"Failed to read tags for '{entry.FileName}': {ex.Message} " +
                        $"— adding partial record.");

                    // Keep a partial record so Task 3 still knows the file exists
                    results.Add(new Mp3Metadata
                    {
                        FileName = entry.FileName,
                        FilePath = entry.FilePath,
                        FileSize = entry.FileSize,
                        CreatedAt = entry.CreatedAt,
                        ExtractedAt = DateTime.UtcNow
                    });
                }
            }

            return results;
        }

        private static Mp3Metadata ExtractOne(Mp3FileEntry entry)
        {
            using var tagFile = TagLib.File.Create(entry.FilePath);
            var tag = tagFile.Tag;
            var audio = tagFile.Properties;

            string defaultTitle = entry.FileName.EndsWith(".mp3",
                StringComparison.OrdinalIgnoreCase)
                ? entry.FileName[..^4]
                : entry.FileName;

            string title = tag.Title ?? defaultTitle;
            var artists = tag.Performers ?? Array.Empty<string>();

            AppLogger.Log("Task2",
                $"Tags — '{entry.FileName}': " +
                $"Title='{title}', Artists='{string.Join(", ", artists)}', " +
                $"Album='{tag.Album}', Year={tag.Year}, " +
                $"Duration={audio.Duration}");

            return new Mp3Metadata
            {
                FileName = entry.FileName,
                FilePath = entry.FilePath,
                FileSize = entry.FileSize,
                CreatedAt = entry.CreatedAt,
                ExtractedAt = DateTime.UtcNow,

                Title = tag.Title ?? defaultTitle,
                Artists = tag.Performers ?? Array.Empty<string>(),
                Album = tag.Album ?? string.Empty,
                AlbumArtist = tag.FirstAlbumArtist ?? string.Empty,
                Language = "Unknown",
                Year = tag.Year,
                Track = tag.Track,
                Genres = tag.Genres ?? Array.Empty<string>(),
                Comment = tag.Comment ?? string.Empty,

                DurationSeconds = (int)audio.Duration.TotalSeconds,
                BitRate = audio.AudioBitrate,
                SampleRate = audio.AudioSampleRate,
                Channels = audio.AudioChannels
            };
        }
    }
}