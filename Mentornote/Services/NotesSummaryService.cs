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

        public NotesSummaryService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> GenerateSummaryAsync(string noteContent, int noteId)
        {
            var chunks = ChunkText(noteContent, 1500);
            var allSummaries = new List<string>();
            var apiKey = _config["OpenAI:ApiKey"].Trim();

            foreach (var chunk in chunks)
            {
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

        List<string> ChunkText(string fullText, int maxChunkSize = 1500)
        {
            var chunks = new List<string>();
            for (int i = 0; i < fullText.Length; i += maxChunkSize)
            {
                var chunk = fullText.Substring(i, Math.Min(maxChunkSize, fullText.Length - i));
                chunks.Add(chunk);
            }
            return chunks;
        }
    }

    
    public class OpenAIResponse
    {
        public Choice[] choices { get; set; }

        public class Choice
        {
            public Message message { get; set; }
        }

        public class Message
        {
            public string content { get; set; }
        }
    }

}
