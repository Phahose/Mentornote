#nullable disable
using Mentornote.Data;
using Mentornote.DTOs;
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using UglyToad.PdfPig;

namespace Mentornote.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class FlashCardsController: ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FlashcardService _flashcardService;
        private readonly CardsServices _pdfReaderService;
        public List<string> ErrorList;

        public FlashCardsController(ApplicationDbContext context, FlashcardService flashcardService, CardsServices pdfReaderService)
        {
            _context = context;
            _flashcardService = flashcardService;
            _pdfReaderService = pdfReaderService;
        }


        [HttpPost("generate-from-pdf")]
        [RequestSizeLimit(10_000_000)] // optional: limit upload size to ~10MB
        public async Task<IActionResult> GenerateFromPdf([FromForm] NotesDto request, int id)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded.");

            var userId = id;

            var text = ExtractText(request.File.OpenReadStream());

            var cards = await _flashcardService.GenerateFromNotes(text);
            var title = cards.FirstOrDefault()?.Title ?? "Flashcard Set";
            var set = _flashcardService.CreateFlashcardSet(title, userId, cards);
            _context.FlashcardSets.Add(set);
            await _context.SaveChangesAsync();
              
            return Ok(set);
        }

        public void DeleteFlashcardSet(int flashcardSetId)
        {
            var flashcards = _context.Flashcards.Where(f => f.FlashcardSetId == flashcardSetId).ToList();
            _context.Flashcards.RemoveRange(flashcards);
            var setToDelete = _context.FlashcardSets.FirstOrDefault(f => f.Id == flashcardSetId);

            if (setToDelete != null)
            {
                _context.FlashcardSets.Remove(setToDelete);
                _context.SaveChanges();
            }
        }


        public string ExtractText(Stream pdfStream)
        {
            using var pdf = PdfDocument.Open(pdfStream);
            var sb = new StringBuilder();

            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            return sb.ToString();
        }
    }
}
