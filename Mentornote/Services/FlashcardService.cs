using Mentornote.Models;

namespace Mentornote.Services
{
    public class FlashcardService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        public async Task<List<Flashcard>> GenerateFromNotes(string notes)
        {
            var apiKey = _config["OpenAI:ApiKey"];
            var prompt = $"Generate flashcards from these notes:\n{notes}\n\nReturn JSON array with 'question' and 'answer'.";
            var lines = notes.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };
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

