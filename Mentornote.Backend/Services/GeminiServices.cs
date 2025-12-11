#nullable disable
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using System.Text;
using System.Text.Json;
namespace Mentornote.Backend.Services
{
    public class GeminiServices
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly RagService _ragService;
        private DateTime _lastSummaryTime = DateTime.MinValue;
        private readonly List<string> _summaries = new();

        public GeminiServices(IConfiguration configuration, RagService ragService)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Gemini:ApiKey"];
            _ragService = ragService;
        }

        public async Task<string> GenerateSuggestionAsync(int appointmentId, SuggestionRequest request)
        {
            // 1️⃣ Get relevant document chunks using the question
            var relevantChunks = await _ragService.GetRelevantChunksAsync( request.UserQuestion, appointmentId);

            string docContext = _ragService.BuildContext(relevantChunks);

            // 2️ Build structured prompt
            string prompt = BuildPrompt(request, docContext);

            // 3️⃣ Prepare Gemini request
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
                    temperature = 0.7,
                    maxOutputTokens = 800
                }
            };

            string json = JsonSerializer.Serialize(payload);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(httpRequest);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Gemini error: {body}");

            using var doc = JsonDocument.Parse(body);
            string suggestion = doc.RootElement.GetProperty("candidates")[0]
                                                .GetProperty("content")
                                                .GetProperty("parts")[0]
                                                .GetProperty("text")
                                                .GetString();

            return suggestion ?? "";
        }

        private string BuildPrompt(SuggestionRequest request, string docContext)
        {
            string memory = string.Join("\n- ", request.MemorySummaries ?? new List<string>());
            string recency = string.Join("\n", request.RecentUtterances ?? new List<string>());
            string question = request.UserQuestion ?? "";

            Console.WriteLine("-----------------------------------------------------------");
            Console.WriteLine($"This was the Queestion: {question}");
            Console.WriteLine($"This are the Recent Utterances: {recency}");
            Console.WriteLine($"This is the memory that the Model Has: {memory}");
            Console.WriteLine("-----------------------------------------------------------");


            return $"""
                        You are acting as the candidate in a live job interview.

                        Your task is to generate a confident, natural spoken reply to the interviewer's CURRENT question.

                        ===========================
                        MEETING MEMORY (Summary of past context)
                        - {memory}
                        ===========================

                        RECENT TRANSCRIPT (last moments of conversation)
                        {recency}
                        ===========================

                        RELEVANT DOCUMENT CONTEXT (resume, notes, job description)
                        {docContext}
                        ===========================

                        CURRENT QUESTION TO ANSWER:
                        "{question}"
                        ===========================

                        INSTRUCTIONS:
                        - Answer ONLY the current question.
                        - Use meeting memory + recent transcript only for context.
                        - Use the resume context when helpful, but do NOT rely exclusively on it.
                        - You may generalize and create believable examples.
                        - DO NOT invent employers, degrees, or dates.
                        - Do NOT repeat the question.
                        - Speak as if you are answering in real time.
                    """;
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

                return summary ?? "Summary generation failed.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in GenerateMeetingSummary: {ex.Message}");
                return $"ERROR: Summary generation failed: {ex.Message}";
            }
        }


        public async Task<List<string>> GenerateRollingSummary(List<Utterance> utterances)
        {
            string summary = "zxcvb";


            try 
            {
                if (utterances == null || utterances.Count == 0)
                {
                    Console.WriteLine("⏳ No utterances available yet — skipping summary.");
                    return _summaries;
                }

                utterances = utterances.Where(u => u != null).ToList();

                string transcript = string.Join(" ", utterances
                    .Where(u => u.TimeStamp > _lastSummaryTime)
                    .Select(u => u.Text));

                if (!transcript.Any())
                {
                    Console.WriteLine("SKIPPING SUMMARY — no new transcript.");
                    return new List<string> {"No new Transript" };
                }
                // Build summary instruction prompt
                string prompt = $"""
                                    Summarize the following meeting segment into 2–3 short bullet points.

                                    Focus ONLY on new information introduced in this segment.
                                    Do NOT repeat information already discussed earlier in the meeting.
                                    Keep it concise, direct, and focused.

                                    Meeting segment:
                                        {transcript}

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
                    throw new Exception($"Gemini Summary Error {response.StatusCode}: {body}");
                }

                // Parse Gemini response
                using var doc = JsonDocument.Parse(body);
                summary = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                _summaries.Add(summary);
                _lastSummaryTime = DateTime.UtcNow;
                return _summaries;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in GenerateMeetingSummary: {ex.Message}");
                return new List<string> { $"ERROR: Summary generation failed: {ex.Message}" };
            }
         
        }

        public List<string> GetSummaries() => _summaries;
    }
}
