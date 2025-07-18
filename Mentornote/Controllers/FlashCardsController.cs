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
    //[Authorize]
    public class FlashCardsController: ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FlashcardService _flashcardService;
        private readonly PdfReaderService _pdfReaderService;
        public List<string> ErrorList;

        public FlashCardsController(ApplicationDbContext context, FlashcardService flashcardService, PdfReaderService pdfReaderService)
        {
            _context = context;
            _flashcardService = flashcardService;
            _pdfReaderService = pdfReaderService;
        }

        [HttpPost("generate-from-notes")]
        public async Task<IActionResult> GenerateFromNotes([FromBody] NotesDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // 🧠 Step 1: Run your NLP/AI logic to convert notes into flashcards
            var flashcards = _flashcardService.GenerateFromNotes(request.Notes);

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

        [HttpPost("generate-from-pdf")]
        [RequestSizeLimit(10_000_000)] // optional: limit upload size to ~10MB
        public async Task<IActionResult> GenerateFromPdf([FromForm] NotesDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded.");

            //  var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var userId = 1;

            var text = _pdfReaderService.ExtractText(request.File.OpenReadStream());

            var cards = _flashcardService.GenerateFromNotes(text);
            var title = $"PDF Set - {DateTime.Now:yyyy-MM-dd HH:mm}";

            var set = _flashcardService.CreateFlashcardSet(title, userId, cards);
            _context.FlashcardSets.Add(set);
            await _context.SaveChangesAsync();

            return Ok(set);
        }


    }
}
