using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend_cs.Models
{
    [Table("Artists")]
    public class Artist
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = null!;
        
        [JsonIgnore]
        public ICollection<Album> Albums { get; set; } = new List<Album>();
    }
}
