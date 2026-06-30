using desktop_server_app.Config;
using desktop_server_app.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace desktop_server_app.Components
{
    /// <summary>
    /// Task 1 — Directory scanner.
    /// Periodically scans the configured directory for .mp3 files and publishes
    /// a single <see cref="Mp3ScanBatch"/> message to <c>playlist-scan-queue</c>.
    /// Does NOT call any other task directly.
    /// </summary>
    internal class ReadDirectoryFilesProgram
    {
        private readonly string _directoryPath;
        private readonly int _intervalSeconds;

        public ReadDirectoryFilesProgram()
        {
            var appSettings = AppConfig.Root.GetSection("AppSettings");
            _directoryPath = appSettings["read-directory"]
                ?? throw new InvalidOperationException("Missing 'AppSettings:read-directory'.");
            _intervalSeconds = int.TryParse(appSettings["interval"], out var i) ? i : 3;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            AppLogger.Log("Task1", $"Started — watching: {_directoryPath}");
            AppLogger.Log("Task1", $"Scan interval: {_intervalSeconds} second(s)");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ScanAndPublishAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AppLogger.Log("Task1", $"Scan error: {ex.Message}");
                }

                AppLogger.Log("Task1", $"Next scan in {_intervalSeconds} second(s)…");
                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), cancellationToken);
            }

            AppLogger.Log("Task1", "Stopped.");
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private async Task ScanAndPublishAsync(CancellationToken ct)
        {
            if (!Directory.Exists(_directoryPath))
            {
                AppLogger.Log("Task1", $"Directory not found: {_directoryPath}");
                return;
            }

            // 1. Read all files, filter to .mp3 only
            string[] allFiles = Directory.GetFiles(
                _directoryPath, "*", SearchOption.TopDirectoryOnly);

            var mp3Files = allFiles
                .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            AppLogger.Log("Task1",
                $"Found {allFiles.Length} file(s) total, {mp3Files.Length} .mp3 file(s).");

            if (mp3Files.Length == 0)
            {
                AppLogger.Log("Task1", "No .mp3 files — nothing to publish.");
                return;
            }

            // 2. Build the lightweight batch (no bytes, just paths + metadata)
            var entries = mp3Files.Select(path =>
            {
                var info = new FileInfo(path);
                return new Mp3FileEntry
                {
                    FileName = info.Name,
                    FilePath = info.FullName,
                    FileSize = info.Length,
                    CreatedAt = info.CreationTimeUtc
                };
            }).ToList();

            var batch = new Mp3ScanBatch
            {
                ScannedAt = DateTime.UtcNow,
                SourceDir = _directoryPath,
                Files = entries
            };

            // 3. Publish ONE message for the whole batch → playlist-scan-queue
            using var connection = await RabbitMqConnection.Factory
                .CreateConnectionAsync(ct);
            using var channel = await connection.CreateChannelAsync(
                cancellationToken: ct);

            await RabbitMqConnection.DeclareQueueAsync(
                channel, RabbitMqConnection.ScanQueue, ct);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(batch));
            var props = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: RabbitMqConnection.ScanQueue,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            AppLogger.Log("Task1",
                $"Published scan batch → '{RabbitMqConnection.ScanQueue}' " +
                $"({mp3Files.Length} file(s)).");
        }
    }
}