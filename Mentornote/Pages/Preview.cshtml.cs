#nullable disable
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using static Azure.Core.HttpHeader;
using Markdig;

namespace Mentornote.Pages
{
    public class PreviewModel : PageModel
    {
        private readonly Helpers _helpers;

        public PreviewModel(Helpers helpers)
        {
            _helpers = helpers;
        }
        public User NewUser { get; set; } = new User();
        public string Email { get; set; } = string.Empty;
        public List<FlashcardSet> FlashcardSets { get; set; } = new();
        public List<Flashcard> Flashcards { get; set; } = new();
        public CardsServices flashcardService = new();
        public NoteSummary noteSummary = new();
        public string HtmlSummary { get; set; } = string.Empty;
        public Note Note = new();
        public List<Note> NotesList { get; set; } = new();
        
        public void OnGet(int noteId)
        {
            HttpContext.Session.SetInt32("SelectedNoteId", noteId);
            noteSummary = flashcardService.GetUserNotesSummary(noteId);
            string fullhtmlSummary;
            fullhtmlSummary = _helpers.ConvertMarkdownToHtml(noteSummary.SummaryText);

            if (fullhtmlSummary.Length > 250)
            {
                HtmlSummary = fullhtmlSummary.Substring(0, 250);
            }
            else
            {
                HtmlSummary = fullhtmlSummary;
            }
            Email = HttpContext.Session.GetString("Email")!;
            if (string.IsNullOrEmpty(Email))
            {
                Response.Redirect("/Start");
                return;
            }
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);
            FlashcardSets = flashcardService.GetUserFlashcards(NewUser.Id);

            NotesList = flashcardService.GetUserNotes(NewUser.Id);
            Note = NotesList.Where(n => n.Id == noteId).FirstOrDefault();

            HttpContext.Session.SetInt32("SelectedNoteId", noteId);

            FlashcardSets = FlashcardSets
                .Where(f => f.NoteId == noteId)
                .ToList();
        }

       
    }
}
