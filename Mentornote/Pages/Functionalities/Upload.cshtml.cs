#nullable disable
using Mentornote.Controllers;
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace Mentornote.Pages.Functionalities
{
    public class UploadModel : PageModel
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

        public UploadModel(FlashCardsController flashCardsController)
        {
            _flashCardsController = flashCardsController;
        }

        public void OnGet()
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("/Login");
                return;
            }

            Email = HttpContext.Session.GetString("Email")!;
            if (string.IsNullOrEmpty(Email))
            {
                Response.Redirect("/Login");
                return;
            }
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);
            FlashcardSets = flashcardService.GetUserFlashcards(NewUser.Id);
        }

        public IActionResult OnPost()
        {
            Email = HttpContext.Session.GetString("Email")!;
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);

            if (!string.IsNullOrEmpty(Submit))
            {
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
                    OnGet();
                    return Page();
                }
            }
            return Page();
        }
    }
}
