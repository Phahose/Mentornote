#nullable disable
namespace Mentornote.Models
{
    public class TestQuestionChoice
    {
        public int Id { get; set; }
        public int TestQuestionId { get; set; }
        public string ChoiceText { get; set; }
        public bool IsCorrect { get; set; }
    }
}
