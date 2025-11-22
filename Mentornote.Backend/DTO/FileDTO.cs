namespace Mentornote.Backend.DTO
{
    public class FileDTO
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public StreamContent FileContent { get; set; }
    }
}
