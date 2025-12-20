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
            string roleBlock = GetAppointmentRoleBlock(request.AppointmenType);

            Console.WriteLine("===============================================");
            Console.WriteLine($"This is the Memory");
            Console.WriteLine($"{memory}");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            Console.WriteLine("===============================================");
            Console.WriteLine($"This is the Question");
            Console.WriteLine($"{question}");
            Console.WriteLine("===============================================");
            Console.WriteLine();
            Console.WriteLine("===============================================");
            Console.WriteLine($"This is the recent Utterance");
            Console.WriteLine($"{recency}");
            Console.WriteLine("===============================================");
            Console.WriteLine();


            switch (request.AppSettings.ResponseFormat)
            {
                case ResponseFormat.BulletPoints:
                    return $"""
                        {roleBlock}

                        Your task is to generate **concise talking points ONLY IF a response is appropriate**.
                        If no substantive response is needed, provide minimal acknowledgment or clarification guidance instead.

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

                        MOST RECENT UTTERANCE (may or may not be a question):
                        "{question}"
                        ===========================

                        RESPONSE DECISION RULE (CRITICAL):
                        First, determine whether the most recent utterance is:
                        A) A direct question
                        B) An implicit question or prompt for explanation
                        C) A statement requiring acknowledgment or clarification
                        D) A conversational transition or non-actionable remark

                        If the utterance is category D:
                        - Do NOT generate a substantive answer
                        - Do NOT invent intent
                        - Provide at most 1–2 bullets suggesting:
                          • a brief acknowledgment, OR
                          • a clarifying follow-up the user could say, OR
                          • no response at all if silence is appropriate

                        CONTEXT PRIORITY RULES (CRITICAL):
                        1. Use document context FIRST if a response is required.
                        2. If the document contains applicable experience, you MUST use it.
                        3. Only generalize if the document is clearly insufficient.
                        4. Use placeholders like [[ADD YOUR EXAMPLE]] instead of guessing.

                        OUTPUT FORMAT:
                        - Respond using **Markdown**
                        - Use **bullet points only**
                        - Each bullet must be expressible in **one spoken breath**
                        - Use **bold** for key ideas
                        - Do NOT write full scripted sentences
                        - Do NOT use code blocks

                        DEPTH GUIDANCE:
                        - Provide **1–5 high-signal bullets**
                        - Fewer strong bullets are better than many weak ones

                        RULES:
                        - Do NOT hallucinate intent
                        - Do NOT invent employers, projects, metrics, or dates
                        - Do NOT force a response where none is needed
                        """;

                case ResponseFormat.Guided:
                    return $"""
                        {roleBlock}

                        Your task is to generate a **clear, human-sounding response ONLY IF a response is appropriate**.
                        If the utterance does not clearly require an answer, generate a brief acknowledgment or clarification instead.

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

                        MOST RECENT UTTERANCE (may or may not be a question):
                        "{question}"
                        ===========================

                        RESPONSE DECISION RULE (CRITICAL):
                        Determine whether the utterance requires:
                        A) A direct answer
                        B) Clarification
                        C) A brief acknowledgment
                        D) No response

                        If C or D:
                        - Keep the response minimal (1–2 sentences max)
                        - Do NOT invent content

                        CONTEXT PRIORITY RULES:
                        1. Ground responses in document context when applicable.
                        2. Reuse concrete language from the document if relevant.
                        3. If context is missing, generalize cautiously.
                        4. Use placeholders like [[INSERT YOUR EXPERIENCE]] instead of guessing.

                        STRUCTURE (ONLY IF ANSWERING):
                        1. **Direct answer** (1–2 sentences)
                        2. **1–2 supporting points or examples**
                        3. Optional brief closing insight

                        LENGTH CONSTRAINT:
                        - Aim for **30–45 seconds spoken**
                        - Stop once the point is clearly made

                        RULES:
                        - Do NOT hallucinate intent
                        - Do NOT invent employers, projects, metrics, or dates
                        - Do NOT restate the utterance
                        """;

                case ResponseFormat.FullScript:
                    return $"""
                            {roleBlock}

                            Your task is to generate a **natural, word-for-word spoken response ONLY IF appropriate**.
                            If the utterance does not clearly require a response, generate a short acknowledgment or clarifying reply instead.

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

                            MOST RECENT UTTERANCE (may or may not be a question):
                            "{question}"
                            ===========================

                            RESPONSE DECISION RULE (CRITICAL):
                            Before responding, decide whether the utterance genuinely calls for a spoken reply.
                            If uncertain, err on the side of **minimal response or silence**.

                            CONTEXT PRIORITY RULES:
                            1. Use document context as the primary source of truth.
                            2. Incorporate specific examples only when relevant.
                            3. Avoid inventing facts or intent.

                            ANSWER STYLE (ONLY IF ANSWERING):
                            - Lead with the answer, not setup
                            - Use short, natural sentences
                            - Sound like a real person speaking aloud
                            - Avoid over-explaining

                            LENGTH CONSTRAINT:
                            - Target **45–60 seconds spoken**
                            - Stop early if the answer is already clear

                            RULES:
                            - Do NOT hallucinate intent
                            - Do NOT invent employers, degrees, metrics, or dates
                            - Do NOT restate the utterance
                            """;

                default:
                    return $"""
                            {roleBlock}

                            Respond **only if a response is appropriate**.

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

                            MOST RECENT UTTERANCE:
                            "{question}"

                            RULES:
                            - Do NOT force a response
                            - Prefer clarity over verbosity
                            - Silence or acknowledgment is acceptable

                            OUTPUT FORMAT:
                            - Markdown
                            - Clean, minimal formatting
                            - No code blocks
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
        private string GetAppointmentRoleBlock(string appointmentType)
        {
            return appointmentType switch
            {
                    "Job Interview" => """
                You are acting as a real-time interview coach.

                GOAL:
                Help the candidate answer questions clearly, confidently, and truthfully
                using their real experience and provided documents.

                CONSTRAINTS:
                - Do NOT invent experience, employers, metrics, or dates
                - Prefer document context over general advice
                - Use placeholders instead of guessing
                """,

                    "Performance Review" => """
                You are acting as a career coach during a performance discussion.

                GOAL:
                Help the user communicate impact, growth, and future goals professionally.

                CONSTRAINTS:
                - Avoid blame or defensiveness
                - Frame challenges as learning
                - Focus on outcomes and behaviors
                """,

                    "Client Meeting" => """
                You are acting as a client-facing meeting assistant.

                GOAL:
                Help the user communicate clearly, professionally, and collaboratively.

                CONSTRAINTS:
                - Do NOT oversell or make guarantees
                - Avoid absolutes unless explicitly stated
                - Focus on alignment and next steps
                """,

                    "Sales Call" => """
                You are acting as a sales conversation assistant.

                GOAL:
                Help the user explain value clearly and respond to objections professionally.

                CONSTRAINTS:
                - Do NOT pressure or exaggerate
                - Avoid manipulative language
                - Be honest about limitations
                """,

                    "Team Meeting" => """
                You are acting as a team communication assistant.

                GOAL:
                Help the user communicate clearly, constructively, and collaboratively.

                CONSTRAINTS:
                - Avoid blame
                - Encourage clarity and shared understanding
                - Focus on decisions and actions
                """,

                    "Technical Interview" => """
                You are acting as a technical interview assistant.

                GOAL:
                Help the candidate explain reasoning, trade-offs, and problem-solving clearly.

                CONSTRAINTS:
                - Do NOT hallucinate system details
                - Prefer explaining thought process over certainty
                - Use structure when helpful
                """,

                    "One-on-One" => """
                You are acting as a one-on-one conversation coach.

                GOAL:
                Help the user communicate openly, thoughtfully, and constructively.

                CONSTRAINTS:
                - Be respectful and balanced
                - Avoid defensiveness
                - Encourage clarity and mutual understanding
                """,

                    "General Conversation" => """
                You are acting as a real-time conversation assistant.

                GOAL:
                Help the user respond clearly, naturally, and appropriately.

                CONSTRAINTS:
                - Avoid guessing or inventing facts
                - Be concise and context-aware
                """,

                    _ => """
                You are acting as a real-time conversation assistant.

                GOAL:
                Help the user respond clearly and appropriately in a live conversation.

                CONSTRAINTS:
                - Avoid guessing or inventing facts
                """
                };
        }



        public List<string> GetSummaries() => _summaries;
    }
}
