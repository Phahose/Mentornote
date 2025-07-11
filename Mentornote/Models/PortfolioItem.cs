namespace Mentornote.Models
{
    public class PortfolioItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User User { get; set; } = new();
    }
}
