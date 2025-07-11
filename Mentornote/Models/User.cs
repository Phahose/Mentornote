namespace Mentornote.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public ICollection<FlashcardSet> FlashcardSets { get; set; } = new List<FlashcardSet>();
        public ICollection<PortfolioItem> PortfolioItems { get; set; } = new List<PortfolioItem>();
    }
}
