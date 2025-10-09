#nullable disable
using Markdig;
using Mentornote.Controllers;
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using static Azure.Core.HttpHeader;

namespace Mentornote.Pages.Functionalities
{
    public class Ask_TutorModel : PageModel
    {
        public string Messages { get; set; } = "Your application description page.";
        public User NewUser { get; set; } = new User();
        public string Email { get; set; } = string.Empty;
        public CardsServices flashcardService = new();
        public NoteSummary noteSummary = new();
        public List<FlashcardSet> FlashcardSets { get; set; } = new();
        public string HtmlSummary { get; set; } = string.Empty;
        public Note Note = new();
        public List<Note> NotesList { get; set; } = new();
        [BindProperty]
        public string Submit { get; set; } = string.Empty;
        [BindProperty]
        public string UserQuestion { get; set; } = string.Empty;
        public List<TutorMessage> TutorMessages { get; set; } = new List<TutorMessage>();


        private readonly TutorServices _tutorService;

        public Ask_TutorModel(HttpClient httpClient, IConfiguration config, Helpers helpers)
        {
            _tutorService = new TutorServices(httpClient, config, helpers);
        }

        public void OnGet(int noteId)
        {
            HttpContext.Session.SetInt32("SelectedNoteId", noteId);

            TutorMessages = flashcardService.GetTutorMessages(noteId, NewUser.Id);

            Email = HttpContext.Session.GetString("Email")!;
            if (string.IsNullOrEmpty(Email))
            {
                Response.Redirect("/Start");
                return;
            }
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);

            NotesList = flashcardService.GetUserNotes(NewUser.Id);

            FlashcardSets = flashcardService.GetUserFlashcards(NewUser.Id);
            FlashcardSets = FlashcardSets
               .Where(f => f.NoteId == noteId)
               .ToList();
            Note = NotesList.Where(n => n.Id == noteId).FirstOrDefault();

            HttpContext.Session.SetInt32("SelectedNoteId", noteId);
        }

        public async Task<IActionResult> OnPost()
        {
            int noteId = HttpContext.Session.GetInt32("SelectedNoteId") ?? 0;
            Email = HttpContext.Session.GetString("Email")!;
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);
            FlashcardSets = flashcardService.GetUserFlashcards(NewUser.Id);

            NotesList = flashcardService.GetUserNotes(NewUser.Id);
            Note = NotesList.Where(n => n.Id == noteId).FirstOrDefault();

            // Validate inputs
            if (noteId == 0 || string.IsNullOrEmpty(UserQuestion) || NewUser == null)
            {
                ModelState.AddModelError("", "Invalid request. Please try again.");
                return Page();
            }

            string aiResponse = await _tutorService.AskQuestionAsync(UserQuestion, noteId, NewUser);

            TutorMessages = flashcardService.GetTutorMessages(noteId, NewUser.Id);
            NotesList = flashcardService.GetUserNotes(NewUser.Id);
            Note = NotesList.FirstOrDefault(n => n.Id == noteId);

            return Page();

        }

    }
}
