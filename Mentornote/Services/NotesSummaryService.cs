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
    public class NotesSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Helpers _helpers;

        public NotesSummaryService(HttpClient httpClient, IConfiguration config, Helpers helpers)
        {
            _httpClient = httpClient;
            _config = config;
            _helpers = helpers;
        }

        public async Task<string> GenerateSummaryAsync(string noteContent, int noteId)
        {
            var chunks = _helpers.ChunkText(noteContent, 1500);
            var allSummaries = new List<string>();
            var apiKey = _config["OpenAI:ApiKey"].Trim();
            int chunkIndex = 0;

            foreach (var chunk in chunks)
            {
                chunkIndex++;
                try
                {
                    var prompt = $"Summarize the following content and structure it using markdown with clear sections, bullet points, and bolded titles:\n\n{chunk}";

                    var requestBody = new
                    {
                        model = "gpt-4",
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        },
                        max_tokens = 300
                    };

                    var requestJson = JsonSerializer.Serialize(requestBody);
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"OpenAI API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                        continue;
                    }

                    var responseContentString = await response.Content.ReadAsStringAsync();
                    var responseContent = JsonSerializer.Deserialize<OpenAIResponse>(responseContentString);

                    if (responseContent?.choices?.Length > 0 &&
                        responseContent.choices[0]?.message?.content != null)
                    {
                        var summary = responseContent.choices[0].message.content.Trim();
                        allSummaries.Add(summary);
                    }
                    else
                    {
                        Console.WriteLine("No valid summary generated from OpenAI.");
                        continue;
                    }

                    // --- Embedding for the same chunk ---
                    await GenerateSummaryEmbedding(chunk, noteId, chunkIndex);

                   
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing chunk: {ex.Message}");
                    continue;
                }
            }

            if (allSummaries.Count == 0)
                return "Summary generation failed for all chunks.";

            // Combine all summaries
            var finalSummary = string.Join("\n\n", allSummaries);

            // Save to DB
            CardsServices cardsServices = new();
            NoteSummary noteSummary = new()
            {
                NoteId = noteId,
                SummaryText = finalSummary
            };
            cardsServices.AddNoteSummary(noteSummary);

            return finalSummary;
        }

        public async Task<string> GenerateFakeSummaryAsync(string noteContent, int noteId)
        {
            // Simulate work so UI doesn’t instantly return
            await Task.Delay(300);

            // Instead of actually chunking + sending to AI,
            // we’ll just fake a "summary" with static markdown content.
            var dummySummary =
             @"# Summary of Notes

            **Main Idea**
            - This is a mock summary of your uploaded notes.
            - In development mode, no AI call is made.

            **Key Points**
            - Point 1: Example content from your notes.
            - Point 2: Another important piece of information.
            - Point 3: Supporting detail to test markdown rendering.

            **Conclusion**
            - This summary is hard-coded and used only for testing.";

            // Save to DB like the real method would
            CardsServices cardsServices = new();
            NoteSummary noteSummary = new()
            {
                NoteId = noteId,
                SummaryText = dummySummary
            };
            cardsServices.AddNoteSummary(noteSummary);

            return dummySummary;
        }


        public async Task GenerateSummaryEmbedding(string chunk, int noteId, int chunkIndex)
        {
            try
            {
                var apiKey = _config["OpenAI:ApiKey"].Trim();

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

                    NoteEmbedding noteEmbedding = new()
                    {
                        NoteId = noteId,
                        ChunkText = chunk,
                        EmbeddingJson = embeddingResult,
                        ChunkIndex = chunkIndex
                    };

                    CardsServices cardsServices = new();
                    cardsServices.AddNoteEmbedding(noteEmbedding);
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


    }


   

}
