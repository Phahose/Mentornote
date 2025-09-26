#nullable disable
namespace Mentornote.Models
{
    public class NoteSummary
    {
        public int Id { get; set; }

        public int NoteId { get; set; } // FK to Notes
        public Note Note { get; set; }

        public string SummaryText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
