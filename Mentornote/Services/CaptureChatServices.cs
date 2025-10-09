using Mentornote.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Mentornote.Services
{
    public class CaptureChatServices
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Helpers _helpers;

        public CaptureChatServices(HttpClient httpClient, IConfiguration config, Helpers helpers)
        {
            _httpClient = httpClient;
            _config = config;
            _helpers = helpers;
        }


        /*public async Task<string> AskSpeechCaptureQuestionAsync(string question, int speechCaptureId, User user)
        {
            var apiKey = _config["OpenAI:ApiKey"].Trim();
            SpeechCaptureServices captureServices = new();

            // --- 1. Get embedding for user question ---
            List<double> questionEmbedding = await _helpers.GetEmbeddingVectorAsync(question, apiKey);
            if (questionEmbedding == null || questionEmbedding.Count == 0)
                return "Could not generate embedding for question.";

            // --- 2. Get stored embeddings for this SpeechCapture summary ---
            var captureEmbeddings = captureServices.GetSpeechCaptureEmbeddingsById(speechCaptureId);

            var scoredChunks = new List<(SpeechCaptureEmbedding embedding, double score)>();

            foreach (var e in captureEmbeddings)
            {
                var chunkVector = ParseEmbedding(e.EmbeddingJson);
                if (chunkVector.Count == 0) continue;

                double score = CosineSimilarity(questionEmbedding, chunkVector);
                scoredChunks.Add((e, score));
            }

            // --- 3. Take top N chunks ---
            var topChunks = scoredChunks
                .OrderByDescending(x => x.score)
                .Take(3)
                .Select(x => x.embedding.ChunkText)
                .ToList();

            if (topChunks.Count == 0)
                return "No relevant sections found for this question.";

            // --- 4. Build context prompt ---
            var context = string.Join("\n\n", topChunks);
            var finalPrompt = $@"
                                Use the following transcribed sections to answer the user's question.
                                Context:
                                {context}

                                Question: {question}
                                Answer:
                            ";

            // --- 5. Send to OpenAI Chat API ---
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are an assistant helping explain transcribed recordings accurately without adding new information." },
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
                return $"OpenAI API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";

            var responseString = await response.Content.ReadAsStringAsync();
            var responseContent = JsonSerializer.Deserialize<OpenAIResponse>(responseString);

            string aiAnswer = responseContent?.choices?[0]?.message?.content?.Trim()
                ?? "No valid response from OpenAI.";

            // --- 6. Save chat history (user + AI message) ---
            SpeechCaptureChat userMessage = new()
            {
                SpeechCaptureId = speechCaptureId,
                UserId = user.Id,
                SenderType = "user",
                Message = question,
                CreatedAt = DateTime.UtcNow
            };
            captureServices.AddSpeechCaptureChat(userMessage);

            SpeechCaptureChat aiMessage = new()
            {
                SpeechCaptureId = speechCaptureId,
                UserId = user.Id,
                SenderType = "ai",
                Message = aiAnswer,
                CreatedAt = DateTime.UtcNow
            };
            captureServices.AddSpeechCaptureChat(aiMessage);

            return aiAnswer;
        }*/

    }
}
