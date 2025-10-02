#nullable disable
using Mentornote.Controllers;
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
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
        public string Status { get; set; } = string.Empty;
        [BindProperty]
        public string Title { get; set; } = string.Empty;
        public List<FlashcardSet> FlashcardSets { get; set; } = new();
        public CardsServices flashcardService = new();
        public NotesSummaryService notesSummaryService;

        private readonly IHubContext<ProcessingHub> _hub;
        public UploadModel(FlashCardsController flashCardsController, IHubContext<ProcessingHub> hub)
        {
            _flashCardsController = flashCardsController;
            _hub = hub;
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

        public async Task<IActionResult> OnPost()
        {

            
            Email = HttpContext.Session.GetString("Email")!;
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);
           // await _hub.Clients.User(NewUser.FirstName).SendAsync("ReceiveProgress", "Shiii IDEK sources...", 1);


            if (!string.IsNullOrEmpty(Submit))
            {
                if (Submit == "Create Study Materials")
                {
                    if (UploadedNote != null && UploadedNote.Length > 0)
                    {
                        var notesDto = new Mentornote.DTOs.NotesDto
                        {
                            File = UploadedNote
                        };
                        var status = _flashCardsController.GenerateFromPdf(notesDto, NewUser.Id, Title).GetAwaiter().GetResult();
                        if (status.ToString().Contains("BadRequest"))
                        {
                            Status = "Error";
                            OnGet();
                            return Page();
                        }
                        else if (status.ToString().Contains("OkObjectResult"))
                        {
                            Status = "Success";
                        }
                    }
                    OnGet();
                    return Page();
                }
            }
            return Page();
        }
    }
}
