#nullable disable
using Mentornote.Data;
using Mentornote.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Mentornote.DTOs;
using Mentornote.Services;

namespace Mentornote.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FlashCardsController: ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FlashcardService _flashcardGenerator;
        public List<string> ErrorList;

        public FlashCardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("generate-from-notes")]
        public async Task<IActionResult> GenerateFromNotes([FromBody] NotesDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // 🧠 Step 1: Run your NLP/AI logic to convert notes into flashcards
            var flashcards = _flashcardGenerator.GenerateFromNotes(request.Notes);

            // 🧠 Step 2: Create a flashcard set with a smart name or timestamp
            var setTitle = $"Auto Set - {DateTime.Now:MMM dd, yyyy HH:mm}";
            var flashcardSet = new FlashcardSet
            {
                Title = setTitle,
                UserId = userId,
                Flashcards = flashcards
            };

            _context.FlashcardSets.Add(flashcardSet);
            await _context.SaveChangesAsync();

            return Ok(flashcardSet);
        }

    }
}
