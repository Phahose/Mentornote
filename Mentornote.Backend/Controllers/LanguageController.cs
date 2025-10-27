#nullable disable
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/transcribe")]
    public class LanguageController : Controller
    {
        private readonly ConversationMemory _memory;
        private readonly Transcribe _transcribe;
        private readonly GeminiServices _gemini;


        private readonly string _assemblyApiKey = "YOUR_ASSEMBLY_AI_KEY";
        public LanguageController(ConversationMemory memory, Transcribe transcribe, GeminiServices gemini)
        {
            _memory = memory;
            _transcribe = transcribe;
            _gemini = gemini;
        }

        [HttpPost("/api/transcribe")]
        public async Task<IActionResult> TranscribeChunk()
        {
            // Save the chunk temporarily
            string meetingId = Request.Headers["X-Meeting-ID"];
            if (string.IsNullOrWhiteSpace(meetingId))
            {
                meetingId = Guid.NewGuid().ToString(); // fallback for testing
            }


            // Read the audio chunk directly into memory (no temp file)
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            byte[] audioBytes = ms.ToArray();

            // Pass the raw audio to your transcription model
            var transcriptList = await _transcribe.DeepGramLiveTranscribe(audioBytes);
            string transcript = transcriptList.LastOrDefault() ?? "[No speech detected]";

            // Append transcript to memory
            _memory.Append(meetingId, transcript);
            string fullContext = _memory.GetTranscript(meetingId);

            // Build prompt for LLM
            string prompt =
                $"Meeting so far:\n{fullContext}\n\n" +
                $"Latest speaker said: \"{transcript}\".\n\n" +
                $"Suggest a concise, context-aware response.";


            string suggestion = await _gemini.GenerateSuggestionAsync(prompt);

            // Return both to overlay
            return Ok(new { text = transcript, suggestion });
        }

        /*  [HttpPost("final")]
          public async Task<IActionResult> FinalSummary()
          {
              string meetingId = Request.Headers["X-Meeting-ID"];

              // read full file (which is now saved locally or uploaded)
              using var fs = new FileStream("final_meeting.wav", FileMode.Open);
              string transcript = await RunWhisperX(fs);

              // use _memory.GetTranscript(meetingId) for the live transcript as context
              string summary = await GetLLMSummary(transcript);
              _memory.Clear(meetingId);

              return Ok(new { summary });
          }*/

        private async Task<string> GetSuggestionFromLLM(string transcript)
        {
            // Example using Ollama
            var http = new HttpClient();
            var content = new StringContent($"{{\"model\":\"llama3\",\"prompt\":\"You are a meeting assistant. Given this sentence: '{transcript}', suggest how to reply.\"}}",
                System.Text.Encoding.UTF8, "application/json");

            var resp = await http.PostAsync("http://localhost:11434/api/generate", content);
            return await resp.Content.ReadAsStringAsync();
        }
        private async Task<string> GetLLMSummary(string transcript)
        {
            // Example using Ollama
            var http = new HttpClient();
            var content = new StringContent($"{{\"model\":\"llama3\",\"prompt\":\"You are a meeting assistant. Given this conversation: '{transcript}', generate me a summary.\"}}",
                System.Text.Encoding.UTF8, "application/json");

            var resp = await http.PostAsync("http://localhost:11434/api/generate", content);
            return await resp.Content.ReadAsStringAsync();
        }
    }
}

