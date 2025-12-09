#nullable disable
using Azure.Core;
using DocumentFormat.OpenXml.Vml;
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/gemini")]
   
    public class GeminiController : Controller
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly RagService _ragService;
        private readonly AudioListener _audioListener;
        private readonly GeminiServices _geminiServices;
        public GeminiController(IConfiguration configuration, RagService ragService, AudioListener audioListener, GeminiServices geminiServices)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Gemini:ApiKey"];
            _ragService = ragService;
            _audioListener = audioListener;
            _geminiServices = geminiServices;
        }

        [HttpPost("suggest/{appointmentId}")]
        [Authorize]
        public async Task<IActionResult> GenerateSuggestionAsync(int appointmentId, [FromBody] SuggestionRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("SuggestionRequest cannot be null.");
                }
                   
                string suggestion = await _geminiServices.GenerateSuggestionAsync(appointmentId, request);

                return Ok(suggestion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Suggestion error: {ex.Message}");
                return StatusCode(500, $"Error generating suggestion: {ex.Message}");
            }
        }


        [HttpPost("upload")]
        public async Task<IActionResult> UploadMeetingFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
               

            var uploadDir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
            Directory.CreateDirectory(uploadDir);

            var filePath = System.IO.Path.Combine(uploadDir, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return metadata
            return Ok(new
            {
                file.FileName,
                file.Length,
                filePath
            });
        }

        [HttpPost("summary/{appointmentId}")]
        public async Task<IActionResult> Summary(int appointmentId, [FromBody] SummaryRequest model)
        {
            var summary = await _geminiServices.GenerateMeetingSummary(appointmentId, model.Transcript);
            return Ok(summary);
        }

    }
}

