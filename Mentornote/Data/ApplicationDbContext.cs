using Mentornote.Models;
using Microsoft.EntityFrameworkCore;

namespace Mentornote.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<FlashcardSet> FlashcardSets { get; set; }
        public DbSet<Flashcard> Flashcards { get; set; }
        public DbSet<PortfolioItem> PortfolioItems { get; set; }
    }
}
