#nullable disable
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Mentornote.Backend
{
    public class Transcribe
    {
        #region AssemblyAITranscription
        private readonly string _assemblyApiKey = "YOUR_ASSEMBLY_AI_KEY";
        public async Task<string> RunAssemblyAIRealtime(byte[] audioBytes)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _assemblyApiKey);

            // 1️⃣ Upload chunk (AssemblyAI requires upload before transcription)
            var uploadResp = await http.PostAsync(
                "https://api.assemblyai.com/v2/upload",
                new ByteArrayContent(audioBytes));

            string uploadUrl = await uploadResp.Content.ReadAsStringAsync();
            uploadUrl = uploadUrl.Trim('"'); // JSON returns quoted string

            // 2️⃣ Start transcription job
            var body = new StringContent(
                $"{{\"audio_url\": \"{uploadUrl}\", \"speaker_labels\": true}}",
                System.Text.Encoding.UTF8,
                "application/json");

            var transcriptResp = await http.PostAsync("https://api.assemblyai.com/v2/transcript", body);
            var transcriptJson = await transcriptResp.Content.ReadAsStringAsync();
            var transcriptId = System.Text.Json.JsonDocument.Parse(transcriptJson)
                                  .RootElement.GetProperty("id").GetString();

            // 3️⃣ Poll until transcription complete
            string transcriptText = "";
            while (true)
            {
                var poll = await http.GetStringAsync($"https://api.assemblyai.com/v2/transcript/{transcriptId}");
                var doc = System.Text.Json.JsonDocument.Parse(poll);
                string status = doc.RootElement.GetProperty("status").GetString();

                if (status == "completed")
                {
                    transcriptText = doc.RootElement.GetProperty("text").GetString();
                    break;
                }
                else if (status == "error")
                {
                    transcriptText = "[Transcription failed]";
                    break;
                }

                await Task.Delay(1000); // wait 1s before next check
            }

            return transcriptText;
        }

        public async Task<string> RunAssemblyAIFinal(string filePath)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _assemblyApiKey);

            // Upload file
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var uploadResp = await http.PostAsync("https://api.assemblyai.com/v2/upload", new ByteArrayContent(bytes));
            string uploadUrl = (await uploadResp.Content.ReadAsStringAsync()).Trim('"');

            // Request transcription with summarization
            var jsonBody = new
            {
                audio_url = uploadUrl,
                speaker_labels = true,
                summarization = true,
                summary_type = "bullets"
            };
            var body = new StringContent(System.Text.Json.JsonSerializer.Serialize(jsonBody), System.Text.Encoding.UTF8, "application/json");

            var resp = await http.PostAsync("https://api.assemblyai.com/v2/transcript", body);
            string json = await resp.Content.ReadAsStringAsync();

            var id = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("id").GetString();

            // Poll until done
            string resultText = "";
            while (true)
            {
                var poll = await http.GetStringAsync($"https://api.assemblyai.com/v2/transcript/{id}");
                var doc = System.Text.Json.JsonDocument.Parse(poll);
                string status = doc.RootElement.GetProperty("status").GetString();

                if (status == "completed")
                {
                    resultText = doc.RootElement.GetProperty("text").GetString();
                    string summary = doc.RootElement.TryGetProperty("summary", out var s)
                        ? s.GetString() : "";
                    resultText += $"\n\nSummary:\n{summary}";
                    break;
                }

                if (status == "error") break;
                await Task.Delay(1000);
            }

            return resultText;
        }

        #endregion

        private readonly HttpClient _httpClient;

        private readonly string _deepgramApiKey;
        private readonly object _lock = new(); // for thread safety
        private readonly List<string> _liveTranscripts = new();
        public Transcribe(IOptions<DeepgramOptions> options)
        {
            _deepgramApiKey = options.Value.ApiKey;

            if (string.IsNullOrWhiteSpace(_deepgramApiKey))
                throw new InvalidOperationException("Deepgram:ApiKey is missing.");


            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", _deepgramApiKey);
        }

        public async Task<List<string>> DeepGramLiveTranscribe(byte[] audioBytes)
        {
            var content = new ByteArrayContent(audioBytes);
            Console.WriteLine($"Audio size: {audioBytes.Length} bytes");

            if (audioBytes.Length == 0)
            {
                return _liveTranscripts;
            }
            Console.WriteLine($"Audio Content: {content} bytes");
            content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

            var response = await _httpClient.PostAsync(
                "https://api.deepgram.com/v1/listen?model=nova-2-general&language=en-US&smart_format=true",
                content
            );

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var results = root.GetProperty("results");
            var channels = results.GetProperty("channels")[0];
            var alternatives = channels.GetProperty("alternatives")[0];
            string transcript = alternatives.GetProperty("transcript").GetString();

            // ✅ Add new transcript to the list (thread-safe)
            if (!string.IsNullOrWhiteSpace(transcript))
            {
                lock (_lock)
                {
                    _liveTranscripts.Add(transcript);
                }
            }

            // ✅ Return the updated list
            return _liveTranscripts;
        }

    }
}
