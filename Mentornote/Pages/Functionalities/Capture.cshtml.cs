#nullable disable
using Mentornote.Models;
using Mentornote.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        public string Summary { get; set; }
        public void OnGet(int captureId)
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

            Summary = _helpers.ConvertMarkdownToHtml(ActiveCapture.SummaryText);

        }

        public void OnPost()
        {

        }
    }
}
