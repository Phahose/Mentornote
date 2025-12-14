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

                            Your task is to generate **concise talking points** the candidate can speak to,
                            NOT a scripted or complete answer.

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

                            CONTEXT PRIORITY RULES (CRITICAL):
                            1. Look for relevant experience in the document context FIRST.
                            2. If the document contains applicable experience, you MUST use it.
                            3. Only generalize if the document is clearly insufficient.
                            4. Use placeholders like [[ADD YOUR EXAMPLE]] instead of guessing.

                            ANSWER STYLE (CRITICAL):
                            - Answer the question **directly**, not indirectly
                            - Focus on *what to say*, not how to say it
                            - Remove any bullet that does not materially help answer the question

                            OUTPUT FORMAT:
                            - Respond using **Markdown**
                            - Use **bullet points only**
                            - Each bullet must be expressible in **one spoken breath**
                            - Use **bold** to highlight key ideas
                            - Do NOT write full scripted sentences
                            - Do NOT use code blocks

                            DEPTH GUIDANCE:
                            - Provide **3–5 high-signal bullets**
                            - Fewer strong bullets are better than many weak ones

                            RULES:
                            - Do NOT invent employers, projects, metrics, or dates
                            - Do NOT repeat or restate the question
                            """;


                case ResponseFormat.Guided:
                    return $"""
                            You are acting as an interview coach helping the candidate form a strong,
                            **human-sounding response**.

                            Your task is to generate a **clear, direct answer** that can be lightly personalized,
                            not an essay or over-polished script.

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

                            CONTEXT PRIORITY RULES (CRITICAL):
                            1. Ground the response in the document context wherever possible.
                            2. Reuse concrete language or examples from the document if relevant.
                            3. If context is missing, generalize cautiously.
                            4. Use placeholders like [[INSERT YOUR EXPERIENCE]] rather than guessing.

                            ANSWER STYLE (CRITICAL):
                            - Start with a **direct answer** in the first sentence
                            - Do NOT lead with background or framing
                            - Avoid filler, hedging, or generic interview phrases
                            - If a sentence does not strengthen the answer, remove it

                            OUTPUT FORMAT:
                            - Respond using **Markdown**
                            - Use short paragraphs or light bulleting
                            - Use **bold** sparingly for emphasis
                            - Do NOT use code blocks

                            STRUCTURE:
                            1. **Direct answer** (1–2 sentences)
                            2. **1–2 supporting points or examples**
                            3. **Brief closing insight** (optional)

                            DEPTH GUIDANCE:
                            - Be concise and intentional
                            - Explain *only* what is necessary to support the answer

                            LENGTH CONSTRAINT:
                            - Aim for **30–45 seconds of spoken response**
                            - Stop once the point is clearly made

                            HUMAN-LIKENESS RULE:
                            - If a sentence sounds like interview prep material, rewrite it

                            RULES:
                            - Do NOT invent employers, projects, metrics, or dates
                            - Do NOT repeat or restate the question
                            """;


                case ResponseFormat.FullScript:
                    return $"""
                            You are acting as the candidate in a live job interview.

                            Your task is to generate a **natural, word-for-word spoken answer**
                            that sounds confident, direct, and human — not rehearsed.

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

                            CONTEXT PRIORITY RULES (CRITICAL):
                            1. Use the document context as the primary source of truth.
                            2. Incorporate specific responsibilities, skills, or examples where relevant.
                            3. Only generalize if the document lacks sufficient detail.
                            4. Use minimal placeholders instead of inventing facts.

                            ANSWER STYLE (CRITICAL):
                            - Lead with the **answer**, not setup
                            - Use short, natural sentences
                            - Avoid over-explaining or narrating your thought process
                            - Sound like a real person speaking out loud

                            OUTPUT FORMAT:
                            - Respond using **Markdown**
                            - Use natural paragraphs suitable for speech
                            - Use **bold** very sparingly
                            - Do NOT use code blocks

                            DEPTH GUIDANCE:
                            - Clearly explain context, actions, and outcomes
                            - Remove any sentence that does not strengthen the answer

                            LENGTH CONSTRAINT:
                            - Target **45–60 seconds spoken**
                            - Stop early if the answer is already clear

                            RULES:
                            - Do NOT invent employers, degrees, metrics, or dates
                            - Do NOT repeat or restate the question
                            """;


                default:
                    return $"""
                            You are acting as the candidate in a live job interview.

                            Answer the interviewer's question **clearly and directly**.

                            ===========================
                            RELEVANT DOCUMENT CONTEXT
                            {docContext}
                            ===========================

                            MEETING MEMORY
                            {memory}
                            ===========================

                            RECENT TRANSCRIPT
                            {recency}
                            ===========================

                            CURRENT QUESTION:
                            "{question}"

                            ANSWER STYLE:
                            - Be direct and concise
                            - Answer first, explain second if needed
                            - Avoid unnecessary detail

                            OUTPUT FORMAT:
                            - Respond using **Markdown**
                            - Keep formatting clean and readable
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
