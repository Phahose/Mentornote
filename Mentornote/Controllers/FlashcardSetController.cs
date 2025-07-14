using Mentornote.Data;
using Mentornote.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mentornote.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserSets(int userId)
        {
            var sets = await _context.FlashcardSets
                .Where(s => s.UserId == userId)
                .Include(s => s.Flashcards)
                .ToListAsync();

            return Ok(sets);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FlashcardSet set)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == set.UserId);
            if (!userExists)
            {
                return BadRequest("Invalid user ID — user does not exist.");
            }
            _context.FlashcardSets.Add(set);
            await _context.SaveChangesAsync();
            return Ok(set);
        }
    }
}
