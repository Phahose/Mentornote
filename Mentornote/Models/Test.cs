#nullable disable
namespace Mentornote.Models
{
    public class Test
    {
        public int Id { get; set; }
        public int NoteId { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<TestQuestion> Questions { get; set; } = new List<TestQuestion>();
    }
}
