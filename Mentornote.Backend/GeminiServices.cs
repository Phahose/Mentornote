#nullable disable
using System.Text;
using System.Text.Json;

namespace Mentornote.Backend
{
    public class GeminiServices
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiServices(IConfiguration config)
        {
            _apiKey = config["Gemini:ApiKey"];
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
            };
        }

        public async Task<string> GenerateSuggestionAsync(string transcript)
        {
            // Gemini 1.5 Flash endpoint
            var url = $"v1beta/models/gemini-2.5-pro:generateContent?key={_apiKey}";

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
                    maxOutputTokens = 5000,
                }
            };

            var json = JsonSerializer.Serialize(prompt);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            string postingURL = _httpClient.BaseAddress + url;
            Console.WriteLine($"🔍 Gemini endpoint: {_httpClient.BaseAddress}{url}");
            var response = await _httpClient.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Gemini error: {response.StatusCode}\n{body}");

            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? string.Empty;
        }

    }
}
