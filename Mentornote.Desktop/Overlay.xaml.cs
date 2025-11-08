#nullable disable
using Mentornote.Backend;
using Mentornote.Desktop.MVVM;
using Mentornote.Models;
using Mentornote.Services;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Mentornote.Desktop
{
    public partial class Overlay : Window
    {
        private AudioListener _listener;
        private bool _isListening = false;
        private static readonly HttpClient _http = new HttpClient();
        private string _meetingId;
       
        public Overlay()
        {
            InitializeComponent();

            // Ensure the overlay fills the primary screen perfectly
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            this.Left = screen.Bounds.Left;
            this.Top = screen.Bounds.Top;
            this.Width = screen.Bounds.Width;
            this.Height = screen.Bounds.Height;

            Loaded += (_, __) => MakeTransparentLayer();
        }

        private void Mic_Click(object sender, RoutedEventArgs e)
        {
            if (!_isListening)
            {
                 _meetingId = Guid.NewGuid().ToString();
                _listener = new AudioListener();
               // _listener.AudioFileReady += ProcessAudioFile;
                _listener.AudioChunkReady += ProcessLiveAudio;
                _listener.StartListening();
                _isListening = true;
                Console.WriteLine("Started capturing system audio...");
                ListeningSection.Visibility = Visibility.Visible;
            }
            else 
            {                
                _listener.StopListening();
                _isListening = false;
                Console.WriteLine("Stopped capturing system audio.");
                RecordingCheck.Text = "Not Recording";
            }
        }

        //private async void Suggestion_Click(object sender, RoutedEventArgs e)
        //{
        //    Helper helper = new();
        //    StatementText.Text = "Generating suggestion...";
        //    var transcript = await helper.GetFullTranscriptAsync();
        //    StatementText.Text = transcript;
        //    var json = JsonSerializer.Serialize(transcript);
        //    var content = new StringContent(json, Encoding.UTF8, "application/json");

        //    var response = await _http.PostAsync("http://localhost:5085/api/gemini/suggest", content);
        //    response.EnsureSuccessStatusCode();



        //    var suggestion = await response.Content.ReadAsStringAsync();
        //    SuggestionText.Text = suggestion;
        //}

        private async void Suggestion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Helper helper = new();
                StatementText.Text = "Generating suggestion...";

                // 1️⃣ Get transcript
                var transcript = await helper.GetFullTranscriptAsync();
                var cleanedTranscript = CleanTranscript(transcript);
                StatementText.Text = string.Join(
                    Environment.NewLine,
                    cleanedTranscript.Split(' ')
                                .TakeLast(30) // only show 30 most recent words
                );

                // 2️⃣ Serialize to JSON
                var json = JsonSerializer.Serialize(cleanedTranscript);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 3️⃣ POST and get streaming response
                var response = await _http.PostAsync("http://localhost:5085/api/gemini/suggest", content);
                response.EnsureSuccessStatusCode();

                var suggestion = await response.Content.ReadAsStringAsync();
                SuggestionText.Text = suggestion;
            }
            catch (Exception ex)
            {
                SuggestionText.Text = $"Error: {ex.Message}";
            }
        }


        private async void ProcessLiveAudio(object sender, byte[] chunk)
        {
            try
            {
                using var content = new ByteArrayContent(chunk);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");


                _http.DefaultRequestHeaders.Remove("X-Meeting-ID");
                _http.DefaultRequestHeaders.Add("X-Meeting-ID", _meetingId);

                // send to your API endpoint (running in Speko.Backend or local ASP.NET)
                var response = await _http.PostAsync("http://localhost:5085/api/transcribe", content);
                response.EnsureSuccessStatusCode();

                // 2️⃣ Parse JSON into usable C# object
                var result = await response.Content.ReadFromJsonAsync<TranscibeResponse>();

                string transcript = result?.Text ?? ""; 

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending chunk: {ex.Message}");
            }

        }

        private string CleanTranscript(string rawTranscript)
        {
            if (string.IsNullOrWhiteSpace(rawTranscript))
                return string.Empty;

            // Remove duplicate lines and trim whitespace
            var cleaned = rawTranscript
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .Distinct() // remove exact duplicates
                .ToList();

            // Join back into a single paragraph
            return string.Join(" ", cleaned);
        }

        private async void ProcessAudioFile(object sender, string filePath)
        {
            RecordedText.Text = "Audio Saved";
            try
            {
                Console.WriteLine($"[Summary] Uploading final audio file: {filePath}");

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                content.Add(fileContent, "file", Path.GetFileName(filePath));

                var response = await _http.PostAsync("https://localhost:5085/api/transcribe/final", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Summary] Response: {json}");

                // optional: delete temp file after use
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending final file: {ex.Message}");
            }
        }

        #region Embedding and Q&A (not used currently)

        /*public async Task GenerateSummaryEmbedding(string chunk, int noteId, int chunkIndex)
        {
            try
            {
                var apiKey = _config["OpenAI:ApiKey"].Trim();

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

                    NoteEmbedding noteEmbedding = new()
                    {
                        NoteId = noteId,
                        ChunkText = chunk,
                        EmbeddingJson = embeddingResult,
                        ChunkIndex = chunkIndex
                    };

                    CardsServices cardsServices = new();
                    cardsServices.AddNoteEmbedding(noteEmbedding);
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
        }*/

        //public async Task<string> AskQuestionAsync(string question, int noteId, User user)
        //{
        //    var apiKey = _config["OpenAI:ApiKey"].Trim();
        //    CardsServices cardsServices = new();

        //    // --- 1. Get embedding for the user question ---
        //    List<double> questionEmbedding = await CreateQuestionEmbeddingVector(question, apiKey);

        //    if (questionEmbedding == null || questionEmbedding.Count == 0)
        //        return "Could not generate embedding for question.";

        //    // --- 2. Get note embeddings from DB ---
        //    var noteEmbeddings = cardsServices.GetNoteEmbeddingsByNoteId(noteId); //  DB method

        //    // --- 3. Score each chunk against the question ---
        //    var scoredChunks = new List<(NoteEmbedding embedding, double score)>();

        //    foreach (var e in noteEmbeddings)
        //    {
        //        var chunkVector = _helpers.ParseEmbedding(e.EmbeddingJson); //  parser
        //        if (chunkVector.Count == 0) continue;

        //        double score = _helpers.CosineSimilarity(questionEmbedding, chunkVector);
        //        scoredChunks.Add((e, score));
        //    }

        //    // --- 4. Pick top N chunks (e.g., 3) ---
        //    var topChunks = scoredChunks
        //        .OrderByDescending(x => x.score)
        //        .Take(3)
        //        .Select(x => x.embedding.ChunkText)
        //        .ToList();

        //    if (topChunks.Count == 0)
        //        return "No relevant chunks found for this question.";

        //    // --- 5. Build context prompt ---
        //    var context = string.Join("\n\n", topChunks);
        //    var finalPrompt = $@"
        //        Use the following notes to answer the question.

        //       Context:
        //       {context}

        //        Question: {question}
        //        Answer:
        //        ";

        //    // --- 6. Call OpenAI chat completions API ---
        //    var requestBody = new
        //    {
        //        model = "gpt-4",
        //        messages = new[]
        //        {
        //            new { role = "system", content = "You are a helpful study assistant. and dont haallucinate outside the Notes" },
        //            new { role = "user", content = finalPrompt }
        //        },
        //        max_tokens = 500
        //    };

        //    var requestJson = JsonSerializer.Serialize(requestBody);
        //    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        //    request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        //    var response = await _httpClient.SendAsync(request);

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        return $"OpenAI API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
        //    }

        //    var responseContentString = await response.Content.ReadAsStringAsync();
        //    var responseContent = JsonSerializer.Deserialize<OpenAIResponse>(responseContentString);

        //    string aiAnswer = responseContent?.choices?[0]?.message?.content?.Trim()
        //           ?? "No valid response from OpenAI.";

        //    // Save chat history
        //    TutorMessage tutorMessage = new()
        //    {
        //        NoteId = noteId,
        //        UserId = user.Id,
        //        Message = question,
        //        Response = aiAnswer,
        //        CreatedAt = DateTime.UtcNow
        //    };


        //    cardsServices.AddTutorMessage(tutorMessage);

        //    return responseContent?.choices?[0]?.message?.content?.Trim()
        //           ?? "No valid response from OpenAI.";
        //}

        #endregion


        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _listener?.StopListening();
            ListeningSection.Visibility = Visibility.Collapsed;
            Close();
        }

        // --- make the background transparent but still allow button clicks ---
        private void MakeTransparentLayer()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Apply only WS_EX_LAYERED (keeps transparency, allows interaction)
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);
        }

        // allow dragging if transparency is disabled for debugging
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;

        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }

   
}
