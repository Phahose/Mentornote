#nullable disable
using Markdig;
using Mentornote.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace Mentornote.Services
{
    public class SpeechCaptureServices
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Helpers _helpers;
        private readonly CardsServices _cardServices = new();

        public SpeechCaptureServices(HttpClient httpClient, IConfiguration config, Helpers helpers, CardsServices cardServices)
        {
            _httpClient = httpClient;
            _config = config;
            _helpers = helpers;
            _cardServices = cardServices;
        }

        public async Task<string> GetTranscriptFromWhisper(string audioPath)
        {
            try
            {
                using var client = new HttpClient();
                using var form = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(audioPath);
                var apiKey = _config["OpenAI:ApiKey"].Trim();


                form.Add(new StreamContent(fileStream), "file", Path.GetFileName(audioPath));
                form.Add(new StringContent("whisper-1"), "model");

                // Building API Request
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = form;

                // send and read
                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                // Whisper returns JSON { "text": "transcribed content" }
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("text").GetString();
            }
            catch (Exception ex)
            {

                return ex.Message;
            }
           
        }

        //  Helper to generate summary
        public async Task<string> GenerateSummaryFromText(string text, int sourceid)
        {
            var chunks = _helpers.ChunkText(text, 1500);
            using var client = new HttpClient();
            var apiKey = _config["OpenAI:ApiKey"].Trim();
            int chunkIndex = 0;
            var summary = string.Empty;
            var allSummaries = new List<string>();

            foreach (var chunk in chunks)
            {
                try
                {
                    var requestBody = new
                    {
                        model = "gpt-4o-mini",
                        messages = new[]
                    {
                        new { role = "system", content = "You are a summarization assistant for lecture transcripts." },
                        new { role = "user", content = $"Summarize the following text and structure it using markdown with clear sections, bullet points, and bolded titles:\n{chunk}" }
                    }
                    };

                    // Building API Request
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

                    if (responseContent?.choices?.Length > 0 &&
                        responseContent.choices[0]?.message?.content != null)
                    {
                        summary = responseContent.choices[0].message.content.Trim();
                        allSummaries.Add(summary);
                    }
                    else
                    {
                        Console.WriteLine("No valid summary generated from OpenAI.");
                        continue;
                    }

                    chunkIndex++;
                    // Generate and store embedding for the chunk
                    await CreateCaptureEmbedding(chunk, apiKey, chunkIndex, sourceid);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing chunk: {ex.Message}");
                }
            }



            if (allSummaries.Count == 0)
                return "Summary generation failed for all chunks.";

            // Combine all summaries
            var finalSummary = string.Join("\n\n", allSummaries);

            // Save to DB
            CardsServices cardsServices = new();
            SpeechCaptureSummary captureSummary = new()
            {
                SpeechCaptureId = sourceid,
                SummaryText = finalSummary
            };

            cardsServices.AddSpeechCaptureSummary(captureSummary);

            return finalSummary;
        }


        public async Task CreateCaptureEmbedding(string chunk, string apiKey, int chunkIndex, int captureId)
        {
            try
            {
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

                    SpeechCaptureEmbedding captureEmbedding = new()
                    {
                        CaptureId = captureId,
                        ChunkIndex = chunkIndex,
                        ChunkText = chunk,
                        Embedding = embeddingResult
                    };

                    CardsServices cardsServices = new();
                    cardsServices.AddSpeechCaptureEmbedding(captureEmbedding);
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


        public bool DeleteCapture(int captureId, int userId)
        {
            // Get file path from DB first (you can write a method or SP to do this)
            List<SpeechCapture> captures = _cardServices.GetAllSpeechCaptures(userId);

            SpeechCapture deletingCapture = captures.Where(c => c.Id == captureId).FirstOrDefault();

            string filePath = deletingCapture.TranscriptFilePath;
            if (filePath != null)
            {

                string fileName = Path.GetFileName(filePath);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Call the stored procedure to delete the note record
                _cardServices.DeleteSpeechCapture(captureId);

                return true;
            }

            return false;
        }


        public async Task<string> AskSpeechCaptureQuestion(string question, int speechCaptureId, User user)
        {
            var apiKey = _config["OpenAI:ApiKey"].Trim();
    

            // --- 1. Get embedding for user question ---
            List<double> questionEmbedding = await CreateQuestionEmbeddingVector(question, apiKey);

            if (questionEmbedding == null || questionEmbedding.Count == 0)
            {
                return "Could not generate embedding for question.";
            }

            // --- 2. Get stored embeddings for this SpeechCapture summary ---
            var captureEmbeddings = _cardServices.GetSpeechCaptureEmbeddings(speechCaptureId);

            var scoredChunks = new List<(SpeechCaptureEmbedding embedding, double score)>();

            foreach (var e in captureEmbeddings)
            {
                var chunkVector = _helpers.ParseEmbedding(e.Embedding);
                if (chunkVector.Count == 0) 
                {
                    continue;
                }

                double score = _helpers.CosineSimilarity(questionEmbedding, chunkVector);
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
            {
                return $"OpenAI API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var responseContent = JsonSerializer.Deserialize<OpenAIResponse>(responseString);

            string aiAnswer = responseContent?.choices?[0]?.message?.content?.Trim()
                ?? "No valid response from OpenAI.";

            // --- 6. Save chat history (user + AI message) ---
            SpeechCaptureChat chat = new()
            {
                SpeechCaptureId = speechCaptureId,
                UserId = user.Id,
                SenderType = "user",
                Message = question,
                Response = aiAnswer,
                CreatedAt = DateTime.UtcNow
            };


            _cardServices.AddSpeechCaptureChat(chat);

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

