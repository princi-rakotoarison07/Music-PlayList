using desktop_server_app.Config;
using desktop_server_app.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace desktop_server_app.Components
{
    /// <summary>
    /// Task 4 — Directory cleaner.
    /// Consumes <see cref="UploadSuccessMessage"/> from <c>playlist-upload-queue</c>
    /// and deletes every .mp3 file in the source directory that was just uploaded.
    /// Only runs after a confirmed upload success — files are never deleted speculatively.
    /// Does NOT call any other task directly.
    /// </summary>
    internal class DeleteDataProgram
    {
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            AppLogger.Log("Task4",
                $"Started — consuming '{RabbitMqConnection.UploadQueue}'.");

            using var connection = await RabbitMqConnection.Factory
                .CreateConnectionAsync(cancellationToken);
            using var channel = await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

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
                    var msg = JsonSerializer.Deserialize<UploadSuccessMessage>(json);

                    if (msg is null || string.IsNullOrWhiteSpace(msg.SourceDir))
                    {
                        AppLogger.Log("Task4", "Received invalid success message — discarding.");
                        await channel.BasicNackAsync(
                            ea.DeliveryTag, false, false, cancellationToken);
                        return;
                    }

                    AppLogger.Log("Task4",
                        $"Upload success confirmed — {msg.TrackCount} track(s) from " +
                        $"'{msg.SourceDir}' uploaded at {msg.UploadedAt:u}. " +
                        $"Starting cleanup.");

                    int deleted = DeleteMp3Files(msg.SourceDir, cancellationToken);

                    AppLogger.Log("Task4",
                        $"Cleanup done — {deleted} file(s) deleted from '{msg.SourceDir}'.");

                    await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                }
                catch (JsonException ex)
                {
                    AppLogger.Log("Task4", $"Bad message format: {ex.Message}");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
                }
                catch (Exception ex)
                {
                    AppLogger.Log("Task4", $"Cleanup error: {ex.Message}");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
                }
            };

            await channel.BasicConsumeAsync(
                queue: RabbitMqConnection.UploadQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            AppLogger.Log("Task4", "Waiting for upload confirmations. Press Ctrl+C to stop.");
            await Task.Delay(Timeout.Infinite, cancellationToken);

            AppLogger.Log("Task4", "Stopped.");
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private static int DeleteMp3Files(string directoryPath, CancellationToken ct)
        {
            if (!Directory.Exists(directoryPath))
            {
                AppLogger.Log("Task4", $"Directory not found — nothing to delete: {directoryPath}");
                return 0;
            }

            string[] files = Directory.GetFiles(directoryPath, "*.mp3", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                AppLogger.Log("Task4", "No .mp3 files found in directory.");
                return 0;
            }

            int deleted = 0;
            int failed = 0;

            const int maxRetries = 5;
            const int delayMilliseconds = 500;

            foreach (var filePath in files)
            {
                ct.ThrowIfCancellationRequested();

                bool success = false;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        File.Delete(filePath);
                        AppLogger.Log("Task4", $"Deleted: {Path.GetFileName(filePath)}");
                        deleted++;
                        success = true;
                        break;
                    }
                    catch (Exception ex) when (attempt < maxRetries)
                    {
                        AppLogger.Log("Task4",
                            $"Retry {attempt}/{maxRetries} for '{Path.GetFileName(filePath)}': {ex.Message}");
                        Thread.Sleep(delayMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Log("Task4",
                            $"Failed to delete '{Path.GetFileName(filePath)}' after {maxRetries} attempts: {ex.Message}");
                        failed++;
                        break;
                    }
                }
            }

            if (failed > 0)
                AppLogger.Log("Task4", $"Warning: {failed} file(s) could not be deleted.");

            return deleted;
        }
    }
}