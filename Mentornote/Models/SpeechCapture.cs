#nullable disable
namespace Mentornote.Models
{
    public class SpeechCapture
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TranscriptFilePath { get; set; }
        public string AudioFilePath { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Title { get; set; }
    }

}
