#nullable disable
using Mentornote.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.ClosedCaptions;

namespace Mentornote.Services
{
    public class YouTubeVideoService
    {
        private readonly YoutubeClient _youtube;
        private readonly string _transcriptFolderPath;
        private readonly Helpers _helpers;

        public YouTubeVideoService(Helpers helpers)
        {
            _youtube = new YoutubeClient();

            // Folder to save transcripts
            _transcriptFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "transcripts");

            if (!Directory.Exists(_transcriptFolderPath))
                Directory.CreateDirectory(_transcriptFolderPath);
            _helpers = helpers;
        }

        public async Task<string> ExtractTranscriptAsync(string videoUrl)
        {
            try
            {
                // Parse the video ID from the link
                var videoId = VideoId.Parse(videoUrl);

                // Get available caption tracks
                var manifest = await _youtube.Videos.ClosedCaptions.GetManifestAsync(videoId);

                // Try to find English captions (or fallback to first available)
                var trackInfo = manifest.TryGetByLanguage("en") ?? manifest.Tracks.FirstOrDefault();
                if (trackInfo == null)
                    throw new Exception("No captions found for this YouTube video.");

                // Fetch all captions
                var track = await _youtube.Videos.ClosedCaptions.GetAsync(trackInfo);
                var transcript = string.Join(" ", track.Captions.Select(c => c.Text));


                return transcript;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YouTubeVideoService] Error: {ex.Message}");
                throw;
            }
        }
    }
}
