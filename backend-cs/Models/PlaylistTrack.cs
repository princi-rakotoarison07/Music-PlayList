using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_cs.Models
{
    [Table("PlaylistTracks")]
    public class PlaylistTrack
    {
        [Key]
        public int Id { get; set; }

        public int PlaylistId { get; set; }
        public Playlist? Playlist { get; set; }

        public int Mp3Id { get; set; }
        public Mp3MetaData? Mp3 { get; set; }

        public int Position { get; set; }
    }
}
