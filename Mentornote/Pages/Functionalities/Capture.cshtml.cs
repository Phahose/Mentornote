#nullable disable
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Mentornote.Pages.Functionalities
{
    public class CaptureModel : PageModel
    {

        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly SpeechCaptureServices _speechCaptureServices;
        private readonly Helpers _helpers;


        public CaptureModel(IWebHostEnvironment env, IConfiguration configuration, SpeechCaptureServices speechCaptureServices, Helpers helpers)
        {
            _env = env;
            _configuration = configuration;
            _speechCaptureServices = speechCaptureServices;
            _helpers = helpers;
        }
        public User NewUser { get; set; } = new User();
        public string Email { get; set; } = string.Empty;
        public List<SpeechCapture> Captures = new();
        public SpeechCapture ActiveCapture= new();
        public CardsServices flashcardService = new();
        public List<SpeechCaptureChat> ChatMessages = new();
        public List<SpeechCaptureSummary> CaptureSummaries = new();
        public SpeechCaptureSummary ActiveSummary = new();
        public string Summary { get; set; }
        public async Task OnGet(int captureId)
        {
            Email = HttpContext.Session.GetString("Email")!;
            if (string.IsNullOrEmpty(Email))
            {
                Response.Redirect("/Start");
                return;
            }
            UsersService usersService = new();
            NewUser = usersService.GetUserByEmail(Email);
            Captures = flashcardService.GetAllSpeechCaptures(NewUser.Id);
            ActiveCapture = Captures.FirstOrDefault();
            CaptureSummaries = flashcardService.GetSpeechCaptureSummaryByCapture(captureId);
            ActiveSummary = CaptureSummaries.FirstOrDefault();
            string summarizedTranscripts;

            if (ActiveSummary == null)
            {
                string transcriptText = GetTextFromFile(ActiveCapture.TranscriptFilePath);

                summarizedTranscripts = await _speechCaptureServices.GenerateSummaryFromText(transcriptText, ActiveCapture.Id);
                Summary = _helpers.ConvertMarkdownToHtml(summarizedTranscripts);
            }
            else
            {
                Summary = _helpers.ConvertMarkdownToHtml(ActiveSummary.SummaryText);
            }
        }

        public void OnPost()
        {

        }

        private string GetTextFromFile(string filePath)
        {
            string fileContent = string.Empty;

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    fileContent = System.IO.File.ReadAllText(filePath);
                }
                else
                {
                    Console.WriteLine($"File not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
            }

            return fileContent;
        }
    }
}
