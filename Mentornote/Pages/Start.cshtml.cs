#nullable disable
using Mentornote.Controllers;
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace Mentornote.Pages.Shared
{
    public class StartModel : PageModel
    {
        private readonly FlashCardsController _flashCardsController;
    
        

        [BindProperty]
        public string Submit { get; set; } = string.Empty;
        [BindProperty]
        public IFormFile UploadedNote { get; set; }     
        public User NewUser { get; set; } = new User();
        public string Email { get; set; } = string.Empty;
        public List<FlashcardSet> FlashcardSets { get; set; } = new();
        public CardsServices flashcardService = new();

        public StartModel(FlashCardsController flashCardsController)
        {
            _flashCardsController = flashCardsController;
        }

        public void OnGet()
        {
            Email = HttpContext.Session.GetString("Email")!;
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);
            FlashcardSets = flashcardService.GetUserFlashcards(NewUser.Id);
        }

        public void OnPost() 
        {
            Email = HttpContext.Session.GetString("Email")!;
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);
            if (Submit == "Upload")
            {
                if (UploadedNote != null && UploadedNote.Length > 0)
                {
                    var notesDto = new Mentornote.DTOs.NotesDto
                    {
                        File = UploadedNote
                    };
                    _flashCardsController.GenerateFromPdf(notesDto, NewUser.Id).GetAwaiter().GetResult();
                }
            }
            OnGet();
        }
    }
}
