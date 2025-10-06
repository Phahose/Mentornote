#nullable disable
using Mentornote.Controllers;
using Mentornote.DTOs;
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
        private readonly YouTubeVideoService _youTubeService;
        private readonly Helpers _helpers;
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
        [BindProperty]
        public string VideoLink { get; set; } 

        private readonly IHubContext<ProcessingHub> _hub;
        public UploadModel(FlashCardsController flashCardsController, IHubContext<ProcessingHub> hub, YouTubeVideoService youTubeVideoService, Helpers helpers)
        {
            _flashCardsController = flashCardsController;
            _hub = hub;
            _youTubeService = youTubeVideoService;
            _helpers = helpers;
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
                        var status = _flashCardsController.GenerateFromPdf(UploadedNote, NewUser.Id, Title, null, "Note").GetAwaiter().GetResult();
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
                else if (Submit == "Upload Link")
                {
                    if (string.IsNullOrEmpty(VideoLink))
                    {
                        ModelState.AddModelError("", "Please provide a YouTube link.");
                        return Page();
                    }

                    try
                    {
                        // 1. Extract transcript and save
                        var transcript = await _youTubeService.ExtractTranscriptAsync(VideoLink);

                        // Save transcript as a text file
                        var filePath = await _helpers.SaveNoteFileAsync(null, transcript);
                        // 2. Create and save Note

                        // Get the absolute path again
                        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);

                 
                        using var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read);

                        IFormFile extractTextFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(absolutePath))
                        {
                            Headers = new HeaderDictionary(),
                            ContentType = "text/plain"
                        };



                        var status = _flashCardsController.GenerateFromPdf(extractTextFile, NewUser.Id, Title, VideoLink, "Video").GetAwaiter().GetResult();
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
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error handling video upload: {ex.Message}");
                        ModelState.AddModelError("", "Unable to extract transcript. Make sure the video has captions enabled.");
                        return Page();
                    }
                }

            }
            return Page();
        }
    }
}
