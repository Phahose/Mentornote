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
        private readonly Helpers _helpers;
        public FlashcardService(HttpClient httpClient, IConfiguration config, Helpers helpers)
        {
            _httpClient = httpClient;
            _config = config;
            _helpers = helpers;
        }

        public async Task<List<Flashcard>> GenerateFromNotes(string notes, int noteId)
        {
            var allFlashcards = new List<Flashcard>();
            var chunks = _helpers.ChunkText(notes, 1500); // You can tweak 1500 depending on what works

            foreach (var chunk in chunks)
            {
                try
                {
                   // var flashcardsFromChunk = await GenerateFlashcardsFromChunk(chunk, noteId);
                    var flashcardsFromChunk = await GenerateFakeFlashcardsFromChunk(chunk, noteId);
                    allFlashcards.AddRange(flashcardsFromChunk);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in chunk {chunk}: {ex.Message}");
                    continue; // skip this one and keep going;
                }     
            }

            return allFlashcards;
        }

        private async Task<List<Flashcard>> GenerateFlashcardsFromChunk(string notesChunk, int noteId)
        {
            var apiKey = _config["OpenAI:ApiKey"].Trim();
            var prompt = $"Generate flashcards from these notes and also a title based on the notes. The title should be 2 words max:\n{notesChunk}\n\nReturn JSON array with 'title', 'question', and 'answer'.";

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
                return ParseResponse(json, noteId);
            }

            throw new Exception("Failed after multiple attempts due to rate limiting.");
        }


        private async Task<List<Flashcard>> GenerateFakeFlashcardsFromChunk(string notesChunk, int noteId)
        {
            // Simulate delay so it feels like "processing"
            await Task.Delay(300);

            // Return hard-coded example flashcards
            return new List<Flashcard>
            {
                new Flashcard
                {
                    NoteId = noteId,
                    Title = "Physics",
                    Front = "What is Newton's First Law?",
                    Back = "An object will remain at rest or in uniform motion unless acted upon by an external force."
                },
                new Flashcard
                {
                    NoteId = noteId,
                    Title = "Physics",
                    Front = "What is kinetic energy?",
                    Back = "The energy an object possesses due to its motion."
                },
                new Flashcard
                {
                    NoteId = noteId,
                    Title = "Physics",
                    Front = "What is potential energy?",
                    Back = "Stored energy based on an object's position or state."
                }
            };
        }

        public FlashcardSet CreateFlashcardSet(string title, int userId, List<Flashcard> cards)
        {
            return new FlashcardSet
            {
                Title = title,
                UserId = userId,
                Flashcards = cards,
                NoteId = cards.FirstOrDefault().NoteId
            };
        }

        private List<Flashcard> ParseResponse(string json, int noteId)
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
                Title = fc.Title,
                NoteId = noteId
            }).ToList();
        }

       

    }
}

