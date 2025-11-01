#nullable disable
using System;
using System.Text;
using System.Text.Json;

namespace Mentornote.Backend
{
    public class GeminiServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ConversationMemory _memory;

        public GeminiServices(IConfiguration config)
        {
            _apiKey = config["Gemini:ApiKey"];
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // prevent hangs
        }

        public async Task<string> GenerateSuggestionAsync(string transcript)
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
                                new { text = $"You are an AI meeting assistant. Suggest one concise, context-aware response for the following conversation:\n\n{transcript}" }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 300,
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
    }
}
