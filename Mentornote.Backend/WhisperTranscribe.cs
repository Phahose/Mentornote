#nullable disable
using System.Diagnostics;

namespace Mentornote.Backend
{
    public class WhisperTranscribe
    {
        private async Task<string> RunWhisperRealtime(byte[] audioBytes)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(audioBytes), "file", "chunk.wav");
            content.Add(new StringContent("whisper-1"), "model");

            using var http = new HttpClient();
            var resp = await http.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
            string result = await resp.Content.ReadAsStringAsync();

            return result; // JSON, can parse into text
        }


        private async Task<string> RunWhisperX(string file)
        {
            // simplest way: call Python WhisperX CLI
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"whisperx_cli.py \"{file}\" --model small --language en",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var process = Process.Start(psi);
            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            // parse output (simplified)
            return output.Contains("Transcription:") ? output.Split("Transcription:")[1].Trim() : output;
        }
    }
}
