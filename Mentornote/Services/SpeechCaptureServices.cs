#nullable disable
using Markdig;
using Mentornote.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace Mentornote.Services
{
    public class SpeechCaptureServices
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Helpers _helpers;
        private readonly CardsServices _cardServices = new();

        public SpeechCaptureServices(HttpClient httpClient, IConfiguration config, Helpers helpers, CardsServices cardServices)
        {
            _httpClient = httpClient;
            _config = config;
            _helpers = helpers;
            _cardServices = cardServices;
        }

        public async Task<string> GetTranscriptFromWhisper(string audioPath)
        {
            try
            {
                using var client = new HttpClient();
                using var form = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(audioPath);
                var apiKey = _config["OpenAI:ApiKey"].Trim();


                form.Add(new StreamContent(fileStream), "file", Path.GetFileName(audioPath));
                form.Add(new StringContent("whisper-1"), "model");

                // Building API Request
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = form;

                // send and read
                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                // Whisper returns JSON { "text": "transcribed content" }
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("text").GetString();
            }
            catch (Exception ex)
            {

                return ex.Message;
            }
           
        }

        //  Helper to generate summary
        public async Task<string> GenerateSummaryFromText(string text, int sourceid)
        {
            var chunks = _helpers.ChunkText(text, 1500);
            using var client = new HttpClient();
            var apiKey = _config["OpenAI:ApiKey"].Trim();
            int chunkIndex = 0;
            var summary = string.Empty;
            var allSummaries = new List<string>();

            foreach (var chunk in chunks)
            {
                try
                {
                    var requestBody = new
                    {
                        model = "gpt-4o-mini",
                        messages = new[]
                    {
                        new { role = "system", content = "You are a summarization assistant for lecture transcripts." },
                        new { role = "user", content = $"Summarize the following text and structure it using markdown with clear sections, bullet points, and bolded titles:\n{chunk}" }
                    }
                    };

                    // Building API Request
                    var requestJson = JsonSerializer.Serialize(requestBody);
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        return $"OpenAI API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
                    }

                    var responseContentString = await response.Content.ReadAsStringAsync();

                    var responseContent = JsonSerializer.Deserialize<OpenAIResponse>(responseContentString);

                    if (responseContent?.choices?.Length > 0 &&
                        responseContent.choices[0]?.message?.content != null)
                    {
                        summary = responseContent.choices[0].message.content.Trim();
                        allSummaries.Add(summary);
                    }
                    else
                    {
                        Console.WriteLine("No valid summary generated from OpenAI.");
                        continue;
                    }

                    chunkIndex++;
                    // Generate and store embedding for the chunk
                    await CreateCaptureEmbedding(chunk, apiKey, chunkIndex, sourceid);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing chunk: {ex.Message}");
                }
            }



            if (allSummaries.Count == 0)
                return "Summary generation failed for all chunks.";

            // Combine all summaries
            var finalSummary = string.Join("\n\n", allSummaries);

            // Save to DB
            CardsServices cardsServices = new();
            SpeechCaptureSummary captureSummary = new()
            {
                SpeechCaptureId = sourceid,
                SummaryText = finalSummary
            };

            cardsServices.AddSpeechCaptureSummary(captureSummary);

            return finalSummary;
        }


        public async Task CreateCaptureEmbedding(string chunk, string apiKey, int chunkIndex, int captureId)
        {
            try
            {
                var requestBody = new
                {
                    input = chunk,
                    model = "text-embedding-3-small"
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var embeddingResult = await response.Content.ReadAsStringAsync();

                    SpeechCaptureEmbedding captureEmbedding = new()
                    {
                        CaptureId = captureId,
                        ChunkIndex = chunkIndex,
                        ChunkText = chunk,
                        Embedding = embeddingResult
                    };

                    CardsServices cardsServices = new();
                    cardsServices.AddSpeechCaptureEmbedding(captureEmbedding);
                }
                else
                {
                    Console.WriteLine($"OpenAI Embedding API Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating embedding: {ex.Message}");
            }

        }


        public bool DeleteCapture(int captureId, int userId)
        {
            // Get file path from DB first (you can write a method or SP to do this)
            List<SpeechCapture> captures = _cardServices.GetAllSpeechCaptures(userId);

            SpeechCapture deletingCapture = captures.Where(c => c.Id == captureId).FirstOrDefault();

            string filePath = deletingCapture.TranscriptFilePath;
            if (filePath != null)
            {

                string fileName = Path.GetFileName(filePath);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Call the stored procedure to delete the note record
                _cardServices.DeleteSpeechCapture(captureId);

                return true;
            }

            return false;
        }

    }

}
