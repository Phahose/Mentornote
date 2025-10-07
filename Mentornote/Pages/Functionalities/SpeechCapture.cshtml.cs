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
    public class SpeechCaptureModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly SpeechCaptureServices _speechCaptureServices;
        private readonly Helpers _helpers;


        public SpeechCaptureModel(IWebHostEnvironment env, IConfiguration configuration, SpeechCaptureServices speechCaptureServices, Helpers helpers)
        {
            _env = env;
            _configuration = configuration;
            _speechCaptureServices = speechCaptureServices;
            _helpers = helpers;
        }
        public User NewUser { get; set; } = new User();
        public string Email { get; set; } = string.Empty;
        public List<FlashcardSet> FlashcardSets { get; set; } = new();
        public CardsServices flashcardService = new();
        [BindProperty]
        public IFormFile UploadedAudio { get; set; }

        [BindProperty]
        public int DurationSeconds { get; set; }
        public List<SpeechCapture> Transcripts = new();
        public SpeechCapture ActiveTranscript = new();
        [BindProperty]
        public string Submit { get; set; } = string.Empty;
        [BindProperty]
        public string Title { get; set; } = string.Empty;
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
           // FlashcardSets = flashcardService.GetUserFlashcards(NewUser.Id);
            Transcripts = flashcardService.GetAllSpeechCaptures(NewUser.Id);
            ActiveTranscript = Transcripts.FirstOrDefault();
        }

        public async Task<IActionResult> OnPost()
        {
            string[] split;
            var action = string.Empty;
            var id = 0;
            if (Submit.Contains('-'))
            {
                split = Submit.Split('-');
                action = split[0];
                id = int.Parse(split[1]);
            }

            if (action == "Delete")
            {
                flashcardService.DeleteSpeechCapture(id);
                return Page();
            }

            if (UploadedAudio == null || UploadedAudio.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "No audio file found.");
                return Page();
            }

            // Save audio file locally
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "recordings");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string audioFileName = Guid.NewGuid().ToString() + Path.GetExtension(UploadedAudio.FileName);
            string audioFilePath = Path.Combine(uploadsFolder, audioFileName);

            using (var stream = new FileStream(audioFilePath, FileMode.Create))
            {
                await UploadedAudio.CopyToAsync(stream);
            }

            // Send to Whisper API to get transcript
            string transcriptText = await _speechCaptureServices.GetTranscriptFromWhisper(audioFilePath);

            
            string summarizedTranscripts = await _helpers.GenerateSummaryFromText(transcriptText);


            Email = HttpContext.Session.GetString("Email")!;
            if (string.IsNullOrEmpty(Email))
            {
                Response.Redirect("/Login");
            }
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);

            SpeechCapture capture = new()
            {
                UserId = NewUser.Id,
                TranscriptFilePath = audioFilePath,
                SummaryText = summarizedTranscripts,
                DurationSeconds = DurationSeconds,
                Title = string.IsNullOrEmpty(Title) ? "Untitled Capture" : Title,
            };

            flashcardService.AddSpeechCapture(capture);
            Transcripts = flashcardService.GetAllSpeechCaptures(NewUser.Id);
            ActiveTranscript = Transcripts.FirstOrDefault();
            return Page(); 
        }
    }
}
