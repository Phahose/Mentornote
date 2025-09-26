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

        public async Task<string> GenerateSummaryAsync(string noteContent)
        {
            var prompt = $"Summarize the following content:\n\n{noteContent}";

            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "user", content = prompt }
            },
                max_tokens = 300
            };

            try
            {
                var apiKey = _config["OpenAI:ApiKey"];
                var requestJson = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";

                var responseContentString = await response.Content.ReadAsStringAsync();
                var responseContent = JsonSerializer.Deserialize<OpenAIResponse>(responseContentString);

                return responseContent?.choices?[0]?.message?.content ?? "No summary generated.";
            }
            catch (Exception ex)
            {
                return $"Exception occurred while generating summary: {ex.Message}";
            }
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
