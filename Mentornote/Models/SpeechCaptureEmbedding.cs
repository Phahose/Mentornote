#nullable disable
namespace Mentornote.Models
{
    public class SpeechCaptureEmbedding
    {
        public int Id { get; set; }
        public int CaptureId { get; set; }
        public string Embedding { get; set; }
        public int ChunkIndex { get; set; }
        public string ChunkText { get; set; }
        public DateTime CreatedAt { get; set; }  
    }
}
