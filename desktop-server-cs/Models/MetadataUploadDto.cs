using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace desktop_server_app.Models
{
    // ── Local DTOs (must match backend definitions) ───────────────────────────

    public class UploadResult
    {
        public string filePath { get; set; } = string.Empty;
    }
    // ── DTOs used for the metadata batch POST ─────────────────────────────────────
    public class MetadataBatchRequest
    {
        public string SourceDir { get; set; } = string.Empty;
        public List<MetadataUploadDto> Tracks { get; set; } = new();
    }
    public class MetadataUploadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;   // server path after upload
        public long FileSize { get; set; }
        public string Title { get; set; } = string.Empty;
        public string[] Artists { get; set; } = Array.Empty<string>();
        public string Album { get; set; } = string.Empty;
        public string AlbumArtist { get; set; } = string.Empty;
        public uint Year { get; set; }
        public uint Track { get; set; }
        public string[] Genres { get; set; } = Array.Empty<string>();
        public string Comment { get; set; } = string.Empty;
        public string Language { get; set; } = "Unknown";
        public int DurationSeconds { get; set; }
        public int BitRate { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    }
}
