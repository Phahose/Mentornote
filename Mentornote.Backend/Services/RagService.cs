#nullable disable
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;

namespace Mentornote.Backend.Services
{
    public class RagService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public RagService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<List<AppointmentDocumentEmbedding>> GetRelevantChunksAsync(string transcript, int appointmentId)
        {
            // 1. Embed the user’s transcript snippet
            List<double> transcriptEmbed = await GenerateStatementEmbeddingAsync(transcript);
            DBServices dBServices = new DBServices();

            // 2. Load all chunks for this meeting
            List<AppointmentDocumentEmbedding> embeddings = await dBServices.GetChunksForAppointment(appointmentId);

            // 3. Score them and store in an anonymous object
            var scored = embeddings.Select(e => new {
                    Chunk = e,
                    Score = CosineSimilarity(transcriptEmbed, e.Vector)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            // 4. If best score < threshold → return no context
            //double threshold = 0.35;
            double threshold = 0.18;

            if (!scored.Any() || scored.First().Score < threshold)
            {
                return new List<AppointmentDocumentEmbedding>();
            }

            // 5. Otherwise return top X chunks
            return scored.Take(3).Select(x => x.Chunk).ToList();
        }

        public double CosineSimilarity(List<double> vectorA, string vectorAppointmentJson)
        {
            List<double> vectorB = ParseEmbeddingString(vectorAppointmentJson);
            double dot = 0.0;
            double magA = 0.0;
            double magB = 0.0;

            for (int i = 0; i < vectorA.Count; i++)
            {
                dot += vectorA[i] * vectorB[i];
                magA += Math.Pow(vectorA[i], 2);
                magB += Math.Pow(vectorB[i], 2);
            }

            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        public async Task<List<double>> GenerateStatementEmbeddingAsync(string statement)
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/embedding-001:embedContent?key={_apiKey}";

            var body = new
            {
                model = "models/embedding-001",
                content = new
                {
                    parts = new[]
                    {
                        new { text = statement }
                    }
                }
            };

            var res = await _httpClient.PostAsJsonAsync(endpoint, body);
            var responseJson = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"Embedding request failed: {res.StatusCode} - {responseJson}");
            }

            // Deserialize into a typed response object
            var parsed = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(responseJson);

            if (parsed == null ||parsed.embedding == null || parsed.embedding.values == null)
            {
                throw new Exception("Invalid embedding response format.");
            }

            return parsed.embedding.values;
        }

        public async Task<string> GetDocumentEmbeddingAsync(string content)
        {
            try
            {
                Console.WriteLine($"Received payload: {JsonSerializer.Serialize(content)}");
                if (content == null || string.IsNullOrWhiteSpace(content))
                {
                    return "Error: Text is null or empty.";
                }
                // 1️⃣ Define endpoint correctly
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/embedding-001:embedContent?key={_apiKey}";

                var body = new
                {
                    model = "models/embedding-001",
                    content = new
                    {
                        parts = new[]
                        {
                             new { text = content }
                        }
                    }
                };

                Console.WriteLine(JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
                // 3️⃣ Make request
                var res = await _httpClient.PostAsJsonAsync(endpoint, body);

                // 4️⃣ Log or handle detailed error info before throwing
                var responseJson = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception($"Embedding request failed: {res.StatusCode} - {responseJson}");
                }
                return responseJson;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Embedding request failed: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public string BuildContext(List<AppointmentDocumentEmbedding> chunks)
        {
            if (chunks == null || chunks.Count == 0)
                return "";

            return string.Join("\n\n", chunks.Select(c => c.ChunkText));
        }

        public List<double> ParseEmbeddingString(string json)
        {
            var parsed = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(json);

            if (parsed == null || parsed.embedding == null || parsed.embedding.values == null)
                throw new Exception("Invalid embedding JSON format");

            return parsed.embedding.values;
        }

    }
}
