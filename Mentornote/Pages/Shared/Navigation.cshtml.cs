#nullable disable
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace Mentornote.Pages.Shared
{
    public class NavigationModel : PageModel
    {
        public User NewUser { get; set; } = new User();
        public string Email { get; set; } = string.Empty;
        public List<FlashcardSet> FlashcardSets { get; set; } = new();
        public List<Flashcard> Flashcards { get; set; } = new();
        public CardsServices flashcardService = new();
        public Note Note = new();
        public List<Note> NotesList { get; set; } = new();
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

            NotesList = flashcardService.GetUserNotes(NewUser.Id);
            Note = NotesList.Where(n => n.Id == noteId).FirstOrDefault();

            HttpContext.Session.SetInt32("SelectedNoteId", noteId);
            FlashcardSets = FlashcardSets
                .Where(f => f.NoteId == noteId)
                .ToList();
        }
    }
}
