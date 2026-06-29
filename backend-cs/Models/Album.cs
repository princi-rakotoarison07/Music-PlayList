using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend_cs.Models
{
    [Table("Albums")]
    public class Album
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = null!;
        
        public int ArtistId { get; set; }
        public Artist? Artist { get; set; }
    }
}
