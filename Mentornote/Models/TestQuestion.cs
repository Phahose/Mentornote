#nullable disable
using System.Text.Json.Serialization;

namespace Mentornote.Models
{
    public class TestQuestion
    {
        public int Id { get; set; }
        public int TestId { get; set; }

        [JsonPropertyName("question")]
        public string QuestionText { get; set; }

        [JsonPropertyName("answer")]
        public string AnswerText { get; set; }

        [JsonPropertyName("choices")]
        public List<string> ChoicesRaw { get; set; } // temporary
        public string QuestionType { get; set; }

        public List<TestQuestionChoice> Choices { get; set; } = new List<TestQuestionChoice>();
    }
}
