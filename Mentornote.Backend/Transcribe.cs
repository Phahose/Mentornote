#nullable disable
using Mentornote.Backend.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Mentornote.Backend
{
    public class Transcribe
    {
        private readonly HttpClient _httpClient;
        private readonly string _deepgramApiKey;
        private readonly object _lock = new(); // for thread safety
        private readonly List<string> _liveTranscripts = new();
        private readonly List<Utterance> _liveUtterances = new();
        public Transcribe(IConfiguration config)
        {
            _deepgramApiKey = config["Deepgram:ApiKey"];

            if (string.IsNullOrWhiteSpace(_deepgramApiKey))
            {
                throw new InvalidOperationException("Deepgram:ApiKey is missing.");
            }

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _deepgramApiKey);
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


            return _liveTranscripts;
        }
        //public async Task<List<Utterance>> DeepGramLiveTranscribe(byte[] audioBytes)
        //{
        //    var content = new ByteArrayContent(audioBytes);
        //    Console.WriteLine($"Audio size: {audioBytes.Length} bytes");

        //    if (audioBytes.Length == 0)
        //    {
        //        return _liveUtterances;
        //    }
        //    Console.WriteLine($"Audio Content: {content} bytes");
        //    content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

        //    var response = await _httpClient.PostAsync(
        //        "https://api.deepgram.com/v1/listen?model=nova-2-general&language=en-US&smart_format=true&diarize=true&utterances=true",
        //        content
        //    );

        //    var json = await response.Content.ReadAsStringAsync();

        //    using var doc = JsonDocument.Parse(json);
        //    var root = doc.RootElement;
        //    var results = root.GetProperty("results");


        //    if (results.TryGetProperty("utterances", out var utterancesElement))
        //    {
        //        foreach (var u in utterancesElement.EnumerateArray())
        //        {
        //            var utterance = new Utterance
        //            {
        //                Speaker = u.GetProperty("speaker").GetInt32(),
        //                Start = u.GetProperty("start").GetDouble(),
        //                End = u.GetProperty("end").GetDouble(),
        //                Text = u.GetProperty("transcript").GetString()
        //            };

        //            // Thread-safe store
        //            if (!string.IsNullOrWhiteSpace(utterance.Text))
        //            {
        //                lock (_lock)
        //                {
        //                    _liveUtterances.Add(utterance);
        //                }
        //            }

        //        }
        //    }

        //    return _liveUtterances;
        //}

    }
}
