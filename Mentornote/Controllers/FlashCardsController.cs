#nullable disable
using AspNetCoreGeneratedDocument;
using Azure.Core;
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
using UglyToad.PdfPig.Outline;

namespace Mentornote.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class FlashCardsController: ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FlashcardService _flashcardService;
        private readonly NotesSummaryService _notesSummaryService;
        private readonly CardsServices _cardServices;
        public List<string> ErrorList;

        public FlashCardsController(ApplicationDbContext context, FlashcardService flashcardService, CardsServices cardsServices, NotesSummaryService notesSummaryService)
        {
            _context = context;
            _flashcardService = flashcardService;
            _cardServices = cardsServices;
            _notesSummaryService = notesSummaryService;
        }


        [HttpPost("generate-from-pdf")]
        [RequestSizeLimit(10_000_000)] // optional: limit upload size to ~10MB
        public async Task<IActionResult> GenerateFromPdf([FromForm] NotesDto request, int id, string title)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded.");

            var userId = id;
            int noteId = AddNote(new Note(), userId, request, title);

            var text = ExtractText(request.File.OpenReadStream());

            var cards = await _flashcardService.GenerateFromNotes(text, noteId);
            var summary = await _notesSummaryService.GenerateSummaryAsync(text, noteId);
            var set = _flashcardService.CreateFlashcardSet(title, userId, cards);
            _context.FlashcardSets.Add(set);
            await _context.SaveChangesAsync();
              
            return Ok(set);
        }



        public int AddNote(Note note,  int userId, [FromForm] NotesDto request, string title )
        {
            try
            {
                int noteId;
                note = new Note
                {
                    FileName = request.File.FileName,
                    UserId = userId,
                    Title = title,
                    FilePath = SaveNoteFileAsync(request.File).Result,
                };
                noteId =  _cardServices.AddNote(note, userId);

                return noteId;
            }
            catch (Exception ex)
            {
                ErrorList.Add(ex.Message);
                return 0;
            }
        }

        public async Task<string> SaveNoteFileAsync(IFormFile uploadedNote)
        {
            if (uploadedNote == null || uploadedNote.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "notes");

            // Create folder if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{uploadedNote.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await uploadedNote.CopyToAsync(stream);
            }

            // Return the relative path to store in DB
            return Path.Combine("uploads", "notes", fileName).Replace("\\", "/");
        }

        public bool DeleteNote (int noteId, int userId)
        {
            // Get file path from DB first (you can write a method or SP to do this)
            List<Note> notes =  _cardServices.GetUserNotes(userId);

            Note deletingNote = notes.Where(n => n.Id == noteId).FirstOrDefault();

            string filePath = deletingNote.FilePath;
            if (filePath != null)
            {

                string fileName = Path.GetFileName(filePath);
                // Remove file from wwwroot
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "notes");
                string fullPath = Path.Combine(uploadsFolder, fileName);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // Call the stored procedure to delete the note record
                _cardServices.DeleteNote(noteId);

                return true;
            }

            return false;
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

        public void UpdateNotetitle(Note note, string newTitle)
        {
            _cardServices.UpdateNote(note, newTitle);    
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
