using desktop_server_app.Config;
using desktop_server_app.Components;

// ── Boot ──────────────────────────────────────────────────────────────────────
AppLogger.Initialize();

string appName = AppConfig.Root["AppSettings:ApplicationName"] ?? "desktop-server-app";
AppLogger.Log("SYSTEM", $"Starting: {appName}");
AppLogger.Log("SYSTEM", "Pipeline: Task1 → [scan-queue] → Task2 → [extract-queue] → Task3 → [upload-queue] → Task4");

// ── Graceful shutdown ─────────────────────────────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    AppLogger.Log("SYSTEM", "Shutdown requested (Ctrl+C).");
    cts.Cancel();
};

// ── Wire up the four independent tasks ───────────────────────────────────────
var task1 = new ReadDirectoryFilesProgram();   // scan dir  → playlist-scan-queue
var task2 = new ExportDataProgram(); // scan-queue → extract tags → playlist-extract-queue
var task3 = new CallApiProgram();         // extract-queue → POST API + upload files → playlist-upload-queue
var task4 = new DeleteDataProgram();      // upload-queue → delete source .mp3 files

// All four run concurrently; none calls another directly — only RabbitMQ connects them.
await Task.WhenAll(
    task1.RunAsync(cts.Token),
    task2.RunAsync(cts.Token),
    task3.RunAsync(cts.Token),
    task4.RunAsync(cts.Token)
);

AppLogger.Close();