#nullable disable
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
        public async Task<IActionResult> GenerateSuggestionAsync(int appointmentId, [FromBody] string transcript)
        {
            try
            {
                // 1️⃣ Get relevant document context
          
                var relevantChunks = await _ragService.GetRelevantChunksAsync(transcript, appointmentId);
                string context = _ragService.BuildContext(relevantChunks); // create your context string

                string prompt = $"""
                                You are acting as the candidate in a live job interview.

                                Your task is to generate a confident, natural spoken reply to the interviewer’s MOST RECENT question.

                                IMPORTANT:
                                - The conversation transcript contains multiple turns.
                                - ONLY the final message (the last line) is the interviewer’s current question.
                                - Ignore all earlier turns except for context about what has already been discussed.

                                HOW TO THINK:
                                - Think like a real professional answering in real time.
                                - Use the resume/context when helpful, but do NOT limit your thinking to it.
                                - You may generalize, infer, or create reasonable examples that fit the candidate’s background.
                                - Do NOT invent new job titles, degrees, employers, or dates.
                                - You MAY invent reasonable professional stories, soft skills, insights, and examples.
                                - Never say “not in my resume” or break character.

                                HOW TO ANSWER:
                                - Speak as if you are the candidate thinking on your feet.
                                - Be confident, conversational, and structured.
                                - Keep answers focused and concise.
                                - Use the resume only when relevant.
                                - If the question is unrelated to the resume, answer intelligently and broadly.

                                TASK:
                                1. Identify the FINAL message from the transcript (the most recent interviewer question).
                                2. Craft a polished spoken response to THAT message only.
                                3. Do NOT repeat the question.
                                4. Respond ONLY with what the user should say next.

                                Relevant documents:
                                {context}

                                Full transcript:
                                {transcript}


                                """;


                // 3️⃣ Prepare Gemini request
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={_apiKey}";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 800
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                // 4️⃣ Send to Gemini
                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Gemini error {response.StatusCode}: {body}");
                    return StatusCode((int)response.StatusCode, body);
                }

                // 5️⃣ Extract model response
                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return Content(text ?? string.Empty, "text/plain");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, $"❌ Gemini request failed: {ex.Message}");
            }
        }


        [HttpPost("upload")]
        public async Task<IActionResult> UploadMeetingFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

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

