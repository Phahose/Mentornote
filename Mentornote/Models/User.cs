#nullable disable
namespace Mentornote.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string AuthProvider { get; set; } = "local";

        public ICollection<FlashcardSet> FlashcardSets { get; set; } = new List<FlashcardSet>();
        public ICollection<PortfolioItem> PortfolioItems { get; set; } = new List<PortfolioItem>();
    }
}
