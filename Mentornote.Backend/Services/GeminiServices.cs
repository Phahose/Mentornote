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
                    temperature = request.AppSettings.Creativity,
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

            string toneInstruction = GetToneInstruction(request.AppSettings.ResponseTone);

            switch (request.AppSettings.ResponseFormat)
            {
                case ResponseFormat.BulletPoints:
                    return $"""
                            You are acting as a real-time interview coach.

                            Your task is to generate **talking points** the candidate can expand on,
                            NOT a full answer.

                            TONE:
                            {toneInstruction}

                            ===========================
                            RELEVANT DOCUMENT CONTEXT (PRIMARY SOURCE)
                            {docContext}
                            ===========================

                            MEETING MEMORY (SECONDARY CONTEXT)
                            {memory}
                            ===========================

                            RECENT TRANSCRIPT
                            {recency}
                            ===========================

                            CURRENT QUESTION:
                            "{question}"
                            ===========================

                            CONTEXT PRIORITY RULES (VERY IMPORTANT):
                            1. You MUST first look for relevant information in the document context.
                            2. If the document contains relevant experience, you MUST use it.
                            3. Only if the document is insufficient may you generalize.
                            4. Use placeholders like [[ADD YOUR EXAMPLE]] instead of guessing.

                            OUTPUT FORMAT:
                            - Respond using **Markdown**
                            - Use **bullet points only**
                            - Each bullet should represent a distinct talking point
                            - Use **bold** to highlight key themes
                            - Do NOT write full, scripted sentences
                            - Do NOT use code blocks

                            DEPTH GUIDANCE:
                            - Provide 4–6 strong, high-quality talking points
                            - Focus on *what to mention*, *why it matters*, or *how to frame it*

                            RULES:
                            - Do NOT invent employers, projects, metrics, or dates
                            - Do NOT repeat the question
                            """;


                case ResponseFormat.Guided:
                    return $"""
                            You are acting as an interview coach helping the candidate craft a strong response.

                            Your task is to generate a **well-structured, expressive answer**
                            that the candidate can personalize.

                            TONE:
                            {toneInstruction}

                            ===========================
                            RELEVANT DOCUMENT CONTEXT (PRIMARY SOURCE)
                            {docContext}
                            ===========================

                            MEETING MEMORY (SECONDARY CONTEXT)
                            {memory}
                            ===========================

                            RECENT TRANSCRIPT
                            {recency}
                            ===========================

                            CURRENT QUESTION:
                            "{question}"
                            ===========================

                            CONTEXT PRIORITY RULES (VERY IMPORTANT):
                            1. Always attempt to ground your response in the document context first.
                            2. Reuse language, examples, or themes from the document where relevant.
                            3. If the document does not provide enough detail, generalize cautiously.
                            4. Use placeholders like [[INSERT YOUR EXPERIENCE]] instead of guessing.

                            OUTPUT FORMAT:
                            - Respond using **Markdown**
                            - Use short paragraphs and/or bullet points
                            - Use **bold** for emphasis
                            - Do NOT use code blocks

                            STRUCTURE:
                            1. **Opening context** (grounded in document experience if possible)
                            2. **2–3 detailed points or examples**
                            3. **Closing reflection** (what this demonstrates about you)

                            DEPTH GUIDANCE:
                            - Be descriptive and explanatory
                            - Explain *why* actions were taken and *what impact* they had

                            RULES:
                            - Do NOT invent employers, projects, metrics, or dates
                            - Do NOT repeat the question
                            """;


                case ResponseFormat.FullScript:
                    return $"""
                            You are acting as the candidate in a live job interview.

                            Your task is to generate a **detailed, word-for-word spoken answer**
                            that could be delivered confidently in real time.

                            TONE:
                            {toneInstruction}

                            ===========================
                            RELEVANT DOCUMENT CONTEXT (PRIMARY SOURCE)
                            {docContext}
                            ===========================

                            MEETING MEMORY (SECONDARY CONTEXT)
                            {memory}
                            ===========================

                            RECENT TRANSCRIPT
                            {recency}
                            ===========================

                            CURRENT QUESTION:
                            "{question}"
                            ===========================

                            CONTEXT PRIORITY RULES (VERY IMPORTANT):
                            1. Use the document context as your primary source of truth.
                            2. Incorporate specific responsibilities, skills, or examples from the document where applicable.
                            3. Only generalize if the document does not contain sufficient detail.
                            4. If details are missing, leave minimal placeholders rather than inventing facts.

                            OUTPUT FORMAT:
                            - Respond using **Markdown**
                            - Use natural paragraphs suitable for speaking aloud
                            - Use **bold** sparingly for emphasis
                            - Do NOT use code blocks

                            DEPTH GUIDANCE:
                            - Be thorough and specific
                            - Clearly explain context, actions, reasoning, and outcomes

                            RULES:
                            - Do NOT invent employers, degrees, metrics, or dates
                            - Do NOT repeat the question
                            """;


                default:
                    return $"""
                            You are acting as the candidate in a live job interview.

                            Answer the interviewer's question clearly and professionally.

                            ===========================
                            MEETING MEMORY
                            {memory}
                            ===========================
                            
                            RECENT TRANSCRIPT
                            {recency}
                            ===========================
                            
                            RELEVANT DOCUMENT CONTEXT
                            {docContext}
                            ===========================
                            
                            CURRENT QUESTION:
                            "{question}"

                            OUTPUT FORMAT:
                            - Respond using **Markdown**
                            - Keep formatting simple and readable
                            - Do NOT use code blocks
                            ===========================
                            """;
            }
        }

        private string GetToneInstruction(ResponseTone tone)
        {
            return tone switch
            {
                ResponseTone.Professional =>
                    "Use a professional, composed, and interview-appropriate tone. Avoid slang.",

                ResponseTone.Polite =>
                    "Use a polite, respectful, and courteous tone. Slightly soften statements without sounding uncertain.",

                ResponseTone.Casual =>
                    "Use a relaxed, natural, conversational tone while remaining appropriate for a professional setting.",

                ResponseTone.Executive =>
                    "Use a confident, concise, executive-level tone. Be decisive, outcome-focused, and direct.",

                _ =>
                    "Use a clear and professional tone."
            };
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
