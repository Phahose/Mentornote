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
namespace Mentornote.Backend.Services
{
    public class GeminiServices
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly RagService _ragService;
        private readonly AudioListener _audioListener;
        public GeminiServices(IConfiguration configuration, RagService ragService, AudioListener audioListener)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Gemini:ApiKey"];
            _ragService = ragService;
            _audioListener = audioListener;
        }

        public async Task<string> GenerateMeetingSummary(int appointmentId, string fullTranscript)
        {
            try
            {
                // Default "no summary" sentinel
                string summary = "zxcvb";

                // Prevent wasting tokens on tiny transcripts
                if (string.IsNullOrWhiteSpace(fullTranscript) || fullTranscript.Length < 50)
                {
                    Console.WriteLine("SKIPPING SUMMARY — transcript too short.");
                    return summary;
                }

                // Build summary instruction prompt
                string prompt = $"""
                                    You are an expert meeting summarizer.

                                    Your task is to produce a clear, accurate, and well-structured summary of the meeting based entirely on the transcript provided.

                                    FOLLOW THESE RULES:

                                    1. Do not hallucinate.
                                    2. Be concise but complete.
                                    3. Organize the summary with useful headings (Overview, Key Points, Decisions, Action Items).
                                    4. Never fabricate information not in the transcript.
                                    5. Interpret messy transcript carefully but never invent facts.

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
                            parts = new[] { new { text = prompt } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.4,
                        maxOutputTokens = 2000
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                // Call Gemini
                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Gemini error {response.StatusCode}: {body}");
                    return $"ERROR: Gemini returned status {response.StatusCode}. Response: {body}";
                }

                // Parse Gemini response
                using var doc = JsonDocument.Parse(body);
                summary = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                Console.WriteLine($"SUMMARY GENERATED: {summary}");

                return summary ?? "Summary generation failed.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in GenerateMeetingSummary: {ex.Message}");
                return $"ERROR: Summary generation failed: {ex.Message}";
            }
        }
    }
}
