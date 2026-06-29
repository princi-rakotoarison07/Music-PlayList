namespace desktop_server_app.Models
{
    // ────────────────────────────────────────────────────────────────────────────
    // Queue 1 → 2  :  playlist-scan-queue
    // Produced by  :  Task1_ReadDirectoryProgram
    // Consumed by  :  Task2_ExtractMetadataProgram
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight file entry — just enough to locate the file on disk.
    /// MP3 bytes are never shipped through the queue.
    /// </summary>
    public class Mp3FileEntry
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// One scan result = one directory sweep (all .mp3 files at once).
    /// </summary>
    public class Mp3ScanBatch
    {
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
        public string SourceDir { get; set; } = string.Empty;
        public List<Mp3FileEntry> Files { get; set; } = new();
    }


    // ────────────────────────────────────────────────────────────────────────────
    // Queue 2 → 3  :  playlist-extract-queue
    // Produced by  :  Task2_ExtractMetadataProgram
    // Consumed by  :  Task3_CallApiProgram
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rich ID3 / audio metadata extracted by TagLib.
    /// FilePath is kept so Task 3 can open the file for upload.
    /// </summary>
    public class Mp3Metadata
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;   // needed by Task 3 upload
        public long FileSize { get; set; }

        // ID3 tags
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string AlbumArtist { get; set; } = string.Empty;
        public uint Year { get; set; }
        public uint Track { get; set; }
        public string[] Genres { get; set; } = Array.Empty<string>();
        public string Comment { get; set; } = string.Empty;

        // Audio properties
        public int DurationSeconds { get; set; }
        public int BitRate { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// One batch of extracted metadata (mirrors the original scan batch).
    /// </summary>
    public class Mp3MetadataBatch
    {
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
        public string SourceDir { get; set; } = string.Empty;
        public List<Mp3Metadata> Tracks { get; set; } = new();
    }


    // ────────────────────────────────────────────────────────────────────────────
    // Queue 3 → 4  :  playlist-upload-queue
    // Produced by  :  Task3_CallApiProgram
    // Consumed by  :  Task4_DeleteDataProgram
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Simple success signal sent after the API accepted all files.
    /// Task 4 uses SourceDir to know which directory to wipe.
    /// </summary>
    public class UploadSuccessMessage
    {
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string SourceDir { get; set; } = string.Empty;
        public int TrackCount { get; set; }
    }
}