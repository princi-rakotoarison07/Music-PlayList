using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend_cs.Models
{
    [Table("Genres")]
    public class Genre
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        
        [JsonIgnore]
        public ICollection<Mp3MetaData> Mp3s { get; set; } = new List<Mp3MetaData>();
    }
}
