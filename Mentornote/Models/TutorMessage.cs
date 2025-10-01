#nullable disable
namespace Mentornote.Models
{
    public class TutorMessage
    {
        public int Id { get; set; }          // DB identity column
        public int NoteId { get; set; }      // FK to Notes
        public int UserId { get; set; }      // FK to Users
        public string Message { get; set; }  // User's question
        public string Response { get; set; } // AI's answer
        public DateTime CreatedAt { get; set; }
    }

}
