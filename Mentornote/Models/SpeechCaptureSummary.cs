#nullable disable
namespace Mentornote.Models
{
    public class SpeechCaptureSummary
    {
        public int Id { get; set; }
        public int SpeechCaptureId { get; set; }
        public string SummaryText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
