using Mentornote.Data;
using Mentornote.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mentornote.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlashcardSetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FlashcardSetController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sets = await _context.FlashcardSets.Include(s => s.Flashcards).ToListAsync();
            return Ok(sets);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FlashcardSet set)
        {
            _context.FlashcardSets.Add(set);
            await _context.SaveChangesAsync();
            return Ok(set);
        }
    }
}
