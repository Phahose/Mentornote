#nullable disable
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

        public SpeechCaptureServices(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
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
    }
}
