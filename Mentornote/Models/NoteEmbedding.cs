#nullable disable
namespace Mentornote.Models
{
    public class NoteEmbedding
    {
        public int Id { get; set; }      
        public int NoteId { get; set; }
        public string ChunkText { get; set; }
        public string EmbeddingJson { get; set; }
        public int ChunkIndex { get; set; }
        public DateTime CreatedAt { get; set; } 
    }
}
