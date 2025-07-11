namespace Mentornote.Models
{
    public class Flashcard
    {
        public int Id { get; set; }
        public string Front { get; set; } = string.Empty;
        public string Back { get; set; } = string.Empty;

        public int FlashcardSetId { get; set; }
        public FlashcardSet FlashcardSet { get; set; } = new();
    }
}
