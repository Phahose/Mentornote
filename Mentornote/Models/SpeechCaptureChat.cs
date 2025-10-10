#nullable disable
namespace Mentornote.Models
{
    public class SpeechCaptureChat
    {
        public int Id { get; set; }
        public int SpeechCaptureId { get; set; }
        public int UserId { get; set; }
        public string SenderType { get; set; } = string.Empty; // "user" or "assistant"
        public string Message { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
