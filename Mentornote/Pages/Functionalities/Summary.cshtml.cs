#nullable disable
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using static Azure.Core.HttpHeader;
using Markdig;

namespace Mentornote.Pages.Functionalities
{
    public class SummaryModel : PageModel
    {
        private readonly Helpers _helpers;
        public SummaryModel(Helpers helpers)
        {
            _helpers = helpers;
        }
        public User NewUser { get; set; } = new User();
        public string Email { get; set; } = string.Empty;
        public CardsServices cardServices = new();
        public NoteSummary noteSummary = new();
        public List<FlashcardSet> FlashcardSets { get; set; } = new();
        public string HtmlSummary { get; set; } = string.Empty;
        public Note Note = new();
        public List<Note> NotesList { get; set; } = new();
        public void OnGet(int noteId)
        {
            HttpContext.Session.SetInt32("SelectedNoteId", noteId);
            noteSummary = cardServices.GetUserNotesSummary(noteId);

            HtmlSummary = _helpers.ConvertMarkdownToHtml(noteSummary.SummaryText);

            Email = HttpContext.Session.GetString("Email")!;
            if (string.IsNullOrEmpty(Email))
            {
                Response.Redirect("/Start");
                return;
            }
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);
            FlashcardSets = cardServices.GetUserFlashcards(NewUser.Id);

            //NotesList = cardServices.GetUserNotes(NewUser.Id);
            //Note = NotesList.Where(n => n.Id == noteId).FirstOrDefault();
            Note = cardServices.GetNoteById(noteId, NewUser.Id);

            HttpContext.Session.SetInt32("SelectedNoteId", noteId);

           
        }


    }
}
