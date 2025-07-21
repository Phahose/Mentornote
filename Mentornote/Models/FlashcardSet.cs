namespace Mentornote.Models
{
    public class FlashcardSet
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = new();

        public List<Flashcard> Flashcards { get; set; } = new List<Flashcard> { };
    }
}