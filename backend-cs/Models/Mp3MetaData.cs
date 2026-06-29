using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_cs.Models
{
    [Table("Mp3MetaData")]
    public class Mp3MetaData
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = "";
        
        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = null!;
        
        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; } = null!;
        
        public long FileSize { get; set; }
        
        public int? ArtistId { get; set; }
        public Artist? Artist { get; set; }
        
        public int? AlbumId { get; set; }
        public Album? Album { get; set; }
        
        public short? Year { get; set; }
        public int DurationSeconds { get; set; }
        public string Comment { get; set; } = "";
        public short? Track { get; set; }
        public int BitRate { get; set; }
        public int SampleRate { get; set; }
        public short Channels { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Genre> Genres { get; set; } = new List<Genre>();
    }
}
