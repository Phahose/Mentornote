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
    public class TutorServices
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Helpers _helpers;

        public TutorServices(HttpClient httpClient, IConfiguration config, Helpers helpers)
        {
            _httpClient = httpClient;
            _config = config;
            _helpers = helpers;
        }
        public List<double> ParseEmbedding(string embeddingJson)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<EmbeddingResponse>(embeddingJson);
                return parsed?.data?[0]?.embedding ?? new List<double>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing embedding JSON: {ex.Message}");
                return new List<double>();
            }
        }

        public static double CosineSimilarity(List<double> vectorA, List<double> vectorB)
        {
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

        public async Task<string> AskQuestionAsync(string question, int noteId, User user)
        {
            var apiKey = _config["OpenAI:ApiKey"].Trim();
            CardsServices cardsServices = new();

            // --- 1. Get embedding for the user question ---
            List<double> questionEmbedding = await CreateQuestionEmbeddingVector(question, apiKey);

            if (questionEmbedding == null || questionEmbedding.Count == 0)
                return "Could not generate embedding for question.";

            // --- 2. Get note embeddings from DB ---
            var noteEmbeddings = cardsServices.GetNoteEmbeddingsByNoteId(noteId); //  DB method

            // --- 3. Score each chunk against the question ---
            var scoredChunks = new List<(NoteEmbedding embedding, double score)>();

            foreach (var e in noteEmbeddings)
            {
                var chunkVector = ParseEmbedding(e.EmbeddingJson); //  parser
                if (chunkVector.Count == 0) continue;

                double score = CosineSimilarity(questionEmbedding, chunkVector);
                scoredChunks.Add((e, score));
            }

            // --- 4. Pick top N chunks (e.g., 3) ---
            var topChunks = scoredChunks
                .OrderByDescending(x => x.score)
                .Take(3)
                .Select(x => x.embedding.ChunkText)
                .ToList();

            if (topChunks.Count == 0)
                return "No relevant chunks found for this question.";

            // --- 5. Build context prompt ---
            var context = string.Join("\n\n", topChunks);
                            var finalPrompt = $@"
                Use the following notes to answer the question.
                
               Context:
               {context}

                Question: {question}
                Answer:
                ";

            // --- 6. Call OpenAI chat completions API ---
            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful study assistant. and dont haallucinate outside the Notes" },
                    new { role = "user", content = finalPrompt }
                },
                max_tokens = 500
            };

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

            string aiAnswer = responseContent?.choices?[0]?.message?.content?.Trim()
                   ?? "No valid response from OpenAI.";

            // Save chat history
            TutorMessage tutorMessage = new()
            {
                NoteId = noteId,
                UserId = user.Id,
                Message = question,
                Response = aiAnswer,
                CreatedAt = DateTime.UtcNow
            };


            cardsServices.AddTutorMessage(tutorMessage);

            return responseContent?.choices?[0]?.message?.content?.Trim()
                   ?? "No valid response from OpenAI.";
        }


        private async Task<List<double>> CreateQuestionEmbeddingVector(string text, string apiKey)
        {
            var requestBody = new
            {
                input = text,
                model = "text-embedding-3-small"
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<double>();

            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var vector = new List<double>();

            foreach (var num in doc.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray())
            {
                vector.Add(num.GetDouble());
            }

            return vector;
        }

    }
}
