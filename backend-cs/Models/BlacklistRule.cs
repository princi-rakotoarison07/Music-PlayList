namespace backend_cs.Models
{
    public class BlacklistRule
    {
        public int Id { get; set; }
        public string RuleType { get; set; } = string.Empty; // 'Artist', 'Genre', 'Title'
        public string Value { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
