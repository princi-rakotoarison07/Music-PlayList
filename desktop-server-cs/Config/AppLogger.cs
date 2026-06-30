using System;
using System.IO;
using desktop_server_app.Config;

namespace desktop_server_app.Config
{
    public static class AppLogger
    {
        private static readonly object _lock = new();
        private static StreamWriter? _writer;
        private static string _logFilePath = string.Empty;

        public static void Initialize()
        {
            var logDir = AppConfig.Root["AppSettings:log-directory"] ?? "logs";
            Directory.CreateDirectory(logDir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _logFilePath = Path.Combine(logDir, $"playlist-log-{timestamp}.log");

            _writer = new StreamWriter(_logFilePath, append: false) { AutoFlush = true };

            Log("SYSTEM", $"Logger initialized. Log file: {_logFilePath}");
        }

        public static void Log(string source, string message)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {message}";

            // Always print to console
            Console.WriteLine(line);

            // Write to file (thread-safe)
            lock (_lock)
            {
                _writer?.WriteLine(line);
            }
        }

        public static void Close()
        {
            Log("SYSTEM", "Logger shutting down.");
            lock (_lock)
            {
                _writer?.Flush();
                _writer?.Close();
                _writer = null;
            }
        }
    }
}