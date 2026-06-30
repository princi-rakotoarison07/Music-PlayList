namespace backend_cs.Models
{
    public class UploadFileRequest
    {
        public IFormFile? File { get; set; }
        public string? Title { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? FileName { get; set; }
    }
}
