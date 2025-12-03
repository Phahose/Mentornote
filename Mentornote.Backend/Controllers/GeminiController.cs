#nullable disable
using DocumentFormat.OpenXml.Vml;
using Mentornote.Backend.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using System.IO;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authorization;

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
        public GeminiController(IConfiguration configuration, RagService ragService, AudioListener audioListener)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Gemini:ApiKey"];
            _ragService = ragService;
            _audioListener = audioListener;
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

                // 2️⃣ Build final RAG-enhanced prompt
                //string prompt = $"""
                //You are assisting a user during a live meeting.

                //Your job is to generate a natural, helpful reply ONLY to the most recent question or statement in the transcript. 
                //Keep the reply short, confident, and appropriate for the situation.

                //USE OF DOCUMENT CONTEXT:
                //- Use information from the attached documents ONLY if it is clearly relevant to the user's most recent statement.
                //- Never invent or alter factual information found in the documents (e.g., job history, dates, credentials).
                //- If the transcript does not relate to the documents, ignore them completely.

                //CREATIVITY RULE:
                //- You may add light conversational details (tone, friendliness, smoothness) to make the reply sound natural.
                //- You may NOT invent new factual details about the user, their background, or any content in the documents.
                //- You may invent details about the users professional experience or knowledge width if there is no relevant information in the document
                //- If context is missing, make a reasonable, generic response without fabricating specific facts.

                //TASK:
                //1. Understand the user's most recent message.
                //2. Decide whether document context applies.
                //3. Produce a single natural reply.

                //---

                //Relevant documents (may be empty or unused):
                //{context}

                //Transcript:
                //{transcript}

                //Respond only with the reply the user should say next.

                //""";

                string prompt = $"""
                                You are assisting a user during a live interview.

                                Your job is to generate a confident, natural verbal reply to the user's most recent message.

                                ASSUMPTIONS:
                                - The documents represent the user's own background (e.g., resume, work history, accomplishments).
                                - You may speak in the first person as if these documents belong to the user.

                                USE OF DOCUMENT CONTEXT:
                                - Use details from the resume when relevant.
                                - You may rephrase, summarize, or expand those details into smooth, natural interview answers.
                                - Do NOT invent new companies, job titles, degrees, or dates.
                                - You MAY infer reasonable strengths, soft skills, and general capabilities if the resume implies them.
                                - If a specific detail is missing, give a strong, general answer rather than freezing.

                                INTERVIEW MODE:
                                - If the message is a common interview question (e.g., “Tell me about yourself”), craft a polished, structured answer using the resume as source material.
                                - Keep the tone confident, conversational, and concise.

                                TASK:
                                1. Understand the user’s most recent message.
                                2. Pull in relevant resume context if appropriate.
                                3. Produce a smooth, natural spoken reply.

                                Relevant documents:
                                {context}

                                Transcript:
                                {transcript}

                                Respond only with the reply the user should say next.
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


        [HttpGet("summary/{appointmentId}")]
        public async Task<IActionResult> GenerateMeetingSummary(int appointmentId)
        {
            try
            {
                string fullTranscript = _audioListener.GetFullMeetingTranscript();
                var summary = "zxcvb";

                if (string.IsNullOrWhiteSpace(fullTranscript) || fullTranscript.Length < 50)
                {
                     Console.WriteLine("SKIPPING SUMMARY — transcript too short.");
                    
                     return Content(summary ?? string.Empty, "text/plain") ;
                }

                if (string.IsNullOrWhiteSpace(fullTranscript))
                {
                    return BadRequest("Transcript is empty.");
                }
                    
                // 1️⃣ Build the summary prompt
                string prompt = $"""
                                    You are an expert meeting summarizer.

                                    Your task is to produce a clear, accurate, and well-structured summary of the meeting based **entirely** on the transcript provided.

                                    FOLLOW THESE RULES:

                                    1. **Do not hallucinate.**  
                                       Only use information explicitly found in the transcript.

                                    2. **Be concise but complete.**  
                                       Capture all major points:
                                       - key topics discussed  
                                       - decisions made  
                                       - action items  
                                       - risks or concerns  
                                       - important context  
                                       - commitments, next steps  

                                    3. **Organize the summary professionally**, using headings such as:
                                       - Overview  
                                       - Key Discussion Points  
                                       - Decisions  
                                       - Action Items  
                                       - Next Steps  

                                    4. **Never fabricate information not present in the transcript.**

                                    5. If the transcript is messy or fragmented (as typical speech-to-text recordings can be), interpret meaning carefully but do not invent facts.

                                    ---

                                    FULL TRANSCRIPT:
                                    {fullTranscript}

                                    Produce the final summary below:
                                 """;

               
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
                        temperature = 0.4,    // lower temp for accuracy & stability
                        maxOutputTokens = 2000
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                // 3️⃣ Call Gemini
                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Gemini error {response.StatusCode}: {body}");
                    return StatusCode((int)response.StatusCode, body);
                }

                // 4️⃣ Parse the model response (same as your example)
                using var doc = JsonDocument.Parse(body);
                summary = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();


                Console.WriteLine($"SUMMARY: {summary}");
                return Content(summary ?? string.Empty, "text/plain");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, $"❌ Gemini summary generation failed: {ex.Message}");
            }
        }


    }
}

