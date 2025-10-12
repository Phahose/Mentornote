#nullable disable
using AspNetCoreGeneratedDocument;
using Azure.Core;
using Mentornote.Data;
using Mentornote.DTOs;
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly TestServices _testServices;
        private readonly CardsServices _cardServices;
        private readonly Helpers _helpers;
        private readonly IHubContext<ProcessingHub> _hub;
        public List<string> ErrorList;

        public FlashCardsController(ApplicationDbContext context, FlashcardService flashcardService, CardsServices cardsServices, NotesSummaryService notesSummaryService, IHubContext<ProcessingHub> hub, TestServices testServices, Helpers helpers)
        {
            _context = context;
            _flashcardService = flashcardService;
            _cardServices = cardsServices;
            _notesSummaryService = notesSummaryService;
            _hub = hub;
            _testServices = testServices;
            _helpers = helpers;
        }


        [HttpPost("generate-from-pdf")]
        [RequestSizeLimit(10_000_000)] // optional: limit upload size to ~10MB
        public async Task<IActionResult> GenerateFromPdf(IFormFile request, int id, string title, string sourceUrl, string sourceType)
        {
            await _hub.Clients.All.SendAsync("ReceiveProgress", "Extracting Your notes...", 1);
            if (request == null || request.Length == 0)
                return BadRequest("No file uploaded.");

            var userId = id;
            int noteId = AddNote(new Note(), userId, request, title, sourceUrl, sourceType);
            

            var text = _helpers.ExtractText(request);

            await _hub.Clients.All.SendAsync("ReceiveProgress", "Making your FlashCards...", 2);

            var cards = await _flashcardService.GenerateFromNotes(text, noteId);

            await _hub.Clients.All.SendAsync("ReceiveProgress", "Summaizing Your Notes...", 3);


            var summary = await _notesSummaryService.GenerateSummaryAsync(text, noteId);


           //var summary = await _notesSummaryService.GenerateFakeSummaryAsync(text, noteId);
            await _hub.Clients.All.SendAsync("ReceiveProgress", "Finalizing...", 4);
            var set = _flashcardService.CreateFlashcardSet(title, userId, cards);
            _context.FlashcardSets.Add(set);
            await _context.SaveChangesAsync();
              
            return Ok(set);
        }



        public int AddNote(Note note,  int userId, IFormFile request, string title, string sourceUrl, string sourceType)
        {
            try
            {
                int noteId;
                note = new Note
                {
                    FileName = request.FileName,
                    UserId = userId,
                    Title = title,
                    SourceUrl = sourceUrl,
                    SourceType = sourceType,
                    FilePath = _helpers.SaveNoteFileAsync(request).Result,
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

    }
}
