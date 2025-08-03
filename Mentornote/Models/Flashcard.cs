using System.Text.Json.Serialization;

namespace Mentornote.Models
{
    public class Flashcard
    {
        public int Id { get; set; }
        [JsonPropertyName("question")]
        public string Front { get; set; } = string.Empty;
        [JsonPropertyName("answer")]
        public string Back { get; set; } = string.Empty;
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        public int FlashcardSetId { get; set; }
      //  public FlashcardSet FlashcardSet { get; set; } = new();
    }
}
