using RabbitMQ.Client;

namespace desktop_server_app.Config
{
    public static class RabbitMqConnection
    {
        public static ConnectionFactory Factory { get; }

        // ── Queue names (one per pipeline stage) ─────────────────────────────────
        /// <summary>Task 1 → Task 2 : raw file list from the directory scan.</summary>
        public static string ScanQueue { get; }

        /// <summary>Task 2 → Task 3 : enriched ID3 metadata batch.</summary>
        public static string ExtractQueue { get; }

        /// <summary>Task 3 → Task 4 : upload-success notification.</summary>
        public static string UploadQueue { get; }

        static RabbitMqConnection()
        {
            var section = AppConfig.Root.GetSection("RabbitMQ");

            Factory = new ConnectionFactory
            {
                HostName = section["HostName"] ?? "localhost",
                UserName = section["UserName"] ?? "guest",
                Password = section["Password"] ?? "guest"
            };

            // Each queue name is derived from a shared base prefix so the config
            // stays simple — override individual names if you need to.
            string prefix = section["QueuePrefix"] ?? "playlist";
            ScanQueue = section["ScanQueue"] ?? $"{prefix}-scan-queue";
            ExtractQueue = section["ExtractQueue"] ?? $"{prefix}-extract-queue";
            UploadQueue = section["UploadQueue"] ?? $"{prefix}-upload-queue";
        }

        // ── Helper : declare a durable queue and return it ───────────────────────
        public static async Task<string> DeclareQueueAsync(
            IChannel channel,
            string queueName,
            CancellationToken ct = default)
        {
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            return queueName;
        }
    }
}