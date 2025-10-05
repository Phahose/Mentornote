#nullable disable
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Text.Json;

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
        [BindProperty]
        public string UserResponses { get; set; }
        public decimal Score { get; set; } = -1;
        [BindProperty]
        public string Submit { get; set; }

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

        public IActionResult OnPost()
        {
            // Get all tests for the note
            int noteId = HttpContext.Session.GetInt32("SelectedNoteId") ?? 0;
            Tests = cardService.GetTestsWithQuestions(noteId);
            ActiveTest = Tests.FirstOrDefault();
            Score = 0;

            if (Submit == "Retake Test")
            {
                Score = -1;
            }
            else if (!string.IsNullOrEmpty(UserResponses))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var responses = JsonSerializer.Deserialize<List<UserResponse>>(UserResponses, options);

                foreach (var response in responses)
                {
                    var question = ActiveTest.Questions.FirstOrDefault(q => q.Id == response.QuestionId); ;
                    var choices = question.Choices;
                    var selectedChoice = choices.Where(c => c.Id == response.OptionId).FirstOrDefault();
                    if (selectedChoice.IsCorrect == true)
                    {
                        Score++;
                    }

                  
                }

                Score = Math.Round((Score * 100) / ActiveTest.Questions.Count, 2);
                

            }
            OnGet(noteId);
            return Page();
        }
    }
}