#nullable disable
namespace Mentornote.Backend.Models
{
    public class AppointmentDocumentEmbedding
    {
        public int EmbeddingId { get; set; }    
        public int AppointmentId { get; set; }
        public int AppointmentDocumentId { get; set; }      
        public int ChunkIndex { get; set; }                
        public string ChunkText { get; set; }              
        public string Vector { get; set; }                 
        public DateTime CreatedAt { get; set; }             
    }
}
