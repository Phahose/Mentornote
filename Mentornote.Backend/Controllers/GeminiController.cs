#nullable disable
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/gemini")]
    public class GeminiController : Controller
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        public GeminiController(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Gemini:ApiKey"];
        }

        [HttpPost("suggest")]
        public async Task<string> GenerateSuggestionAsync([FromBody] string transcript)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key={_apiKey}";

                var prompt = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = $"You are sitting in this meeting. Reply with one simple friendly response to the last statment, in this transcript :\n\n{transcript}" }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 5000,
                    }
                };

                var json = JsonSerializer.Serialize(prompt);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine("➡️ Sending request to Gemini...");
                var response = await _httpClient.PostAsync(url, content);
                Console.WriteLine($"⬅️ Response: {response.StatusCode}");

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Gemini error {response.StatusCode}: {body}");
                    return $"Error: {response.StatusCode}";
                }

                using var doc = JsonDocument.Parse(body);

                Console.WriteLine($"The Body{body}");
                Console.WriteLine($"The Doc{doc}");

                var text = doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                return text ?? string.Empty;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("❌ Timeout: Gemini took too long to respond.");
                return "[Timeout]";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Gemini request failed: {ex.Message}");
                return "[Error contacting Gemini]";
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadMeetingFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, file.FileName);
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

    }
}
