using Mentornote.Models;

namespace Mentornote.Services
{
    public class FlashcardService
    {
        public List<Flashcard> GenerateFromNotes(string notes)
        {
            var lines = notes.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return GenerateFromLines(lines);
        }

        public List<Flashcard> GenerateFromLines(string[] lines)
        {
            var cards = new List<Flashcard>();

            foreach (var line in lines)
            {
                cards.Add(new Flashcard
                {
                    Front = line.Trim(),
                    Back = "TBD" // Replace with logic later
                });
            }

            return cards;
        }

        public FlashcardSet CreateFlashcardSet(string title, int userId, List<Flashcard> cards)
        {
            return new FlashcardSet
            {
                Title = title,
                UserId = userId,
                Flashcards = cards
            };
        }
    }
}
}
