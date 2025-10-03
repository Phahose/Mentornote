#nullable disable
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace Mentornote.Pages.Functionalities
{
    public class PracticeTestModel : PageModel
    {
        private readonly TestServices _testServices;

        public User NewUser { get; set; } = new User();
        public string Email { get; set; } = string.Empty;
        public List<FlashcardSet> FlashcardSets { get; set; } = new();
        public List<Flashcard> Flashcards { get; set; } = new();
        public CardsServices cardService = new();
        public List<Note> NotesList { get; set; } = new();
        public Note Note = new();
        public List<Test> Tests { get; set; } = new();
        public Test ActiveTest { get; set; } = new Test();  


        public PracticeTestModel(TestServices testServices)
        {
            _testServices = testServices;
        }

        public void OnGet(int noteId)
        {
            HttpContext.Session.SetInt32("SelectedNoteId", noteId);

            Email = HttpContext.Session.GetString("Email")!;
            if (string.IsNullOrEmpty(Email))
            {
                Response.Redirect("/Start");
                return;
            }
            UsersService usersService = new();

            NewUser = usersService.GetUserByEmail(Email);

            FlashcardSets = cardService.GetUserFlashcards(NewUser.Id);
            FlashcardSets = FlashcardSets
               .Where(f => f.NoteId == noteId)
               .ToList();

            Note = cardService.GetNoteById(noteId, NewUser.Id);

            // Get all tests for the note
            Tests = cardService.GetTestsWithQuestions(noteId);
            ActiveTest = Tests.FirstOrDefault();


            if (Tests.Count == 0)
            {
                _testServices.CreateTestQuestion(noteId, NewUser.Id).Wait();
            }


            HttpContext.Session.SetInt32("SelectedNoteId", noteId);
        }
    }
}