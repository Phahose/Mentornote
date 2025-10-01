#nullable disable
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace Mentornote.Pages.Functionalities
{
    public class FlashCardsModel : PageModel
    {
        public User NewUser { get; set; } = new User();
        public string Email { get; set; } = string.Empty;
        public List<FlashcardSet> FlashcardSets { get; set; } = new();
        public List<Flashcard> Flashcards { get; set; } = new();
        public CardsServices flashcardService = new();
        public List<Note> NotesList { get; set; } = new();
        public Note Note = new();
        public void OnGet(int flashcardSetId)
        {
            HttpContext.Session.SetInt32("SelectedFlashcardSetId", flashcardSetId);
            Email = HttpContext.Session.GetString("Email")!;
            if (string.IsNullOrEmpty(Email))
            {
                Response.Redirect("/Start");
                return;
            }
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);
            FlashcardSets = flashcardService.GetUserFlashcards(NewUser.Id);

            HttpContext.Session.SetInt32("SelectedFlashcardSetId", flashcardSetId);

            FlashcardSets = FlashcardSets
                .Where(f => f.Id == flashcardSetId)
                .ToList();

            NotesList = flashcardService.GetUserNotes(NewUser.Id);
            Note = NotesList.Where(n => n.Id == FlashcardSets[0].NoteId).FirstOrDefault();

            HttpContext.Session.SetInt32("SelectedNoteId", FlashcardSets[0].NoteId);
        }
    }
}
