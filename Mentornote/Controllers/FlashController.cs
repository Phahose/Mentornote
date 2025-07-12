using Mentornote.Data;

namespace Mentornote.Controllers
{
    public class FlashController
    {
        private readonly ApplicationDbContext _context;

        public FlashController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("addCard")]
        public void AddCard
    }
}
