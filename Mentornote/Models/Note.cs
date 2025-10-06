#nullable disable
namespace Mentornote.Models
{
    public class Note
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; } 

        public string FileName { get; set; } 

        public string FilePath { get; set; } 

        public string SourceType { get; set; }
        public string SourceUrl { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to User
        public int UserId { get; set; }

        // Navigation properties
        public ICollection<FlashcardSet> FlashcardSets { get; set; }
        public ICollection<NoteSummary> Summaries { get; set; }

    }
}
