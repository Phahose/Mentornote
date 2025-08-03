#nullable disable
using Mentornote.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Mentornote.Services
{
    public class FlashcardService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        public FlashcardService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }
        public async Task<List<Flashcard>> GenerateFromNotes(string notes)
        {
           // await Task.Delay(2000);
            var apiKey = _config["OpenAI:ApiKey"].Trim();
            var prompt = $"Generate flashcards from these notes and also a title based on the notes the title should be 2 words max per title:\n{notes}\n\nReturn JSON array with 'title' 'question' and 'answer'.";
            var lines = notes.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            for (int attempt = 0; attempt < 3; attempt++)
            {
                var requestJson = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    Console.WriteLine("Rate limit hit. Retrying in 2 seconds...");
                    await Task.Delay(2000);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseResponse(json);
            }

            throw new Exception("Failed after multiple attempts due to rate limiting.");

        }

        public FlashcardSet CreateFlashcardSet(string title, int userId, List<Flashcard> cards)
        {
            return new FlashcardSet
            {
                Title = title,
                UserId = userId,
                Flashcards = cards
            };
        }

        private List<Flashcard> ParseResponse(string json)
        {
            using var doc = JsonDocument.Parse(json);

            // Navigate to choices[0].message.content
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // content is a JSON string (escaped) — now deserialize it
            var rawFlashcards = JsonSerializer.Deserialize<List<Flashcard>>(content);

            return rawFlashcards.Select(fc => new Flashcard
            {
                Front = fc.Front,
                Back = fc.Back,
                Title = fc.Title
            }).ToList();
        }

       

    }
}

