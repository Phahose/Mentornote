#nullable disable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/transcribe")]
    public class TranscribeController : Controller
    {
        private readonly ConversationMemory _memory = new();
        private readonly Transcribe _transcribe;
        private readonly AudioListener _audioListener;

        public TranscribeController(ConversationMemory memory, Transcribe transcribe, AudioListener audioListener)
        {
            _memory = memory;
            _transcribe = transcribe;
            _audioListener = audioListener;
        }

        [HttpGet("context")]
        [Authorize]
        public string GetFullTranscript()
        {
            IEnumerable<string> meetingIDList = _memory.GetAllMeetingIds();
            var latestMeetingId = meetingIDList.FirstOrDefault();

            if (latestMeetingId != null)
            {
                var transcript = _memory.GetTranscript(latestMeetingId);
                Console.WriteLine($"Transcript for {latestMeetingId}: {transcript}");
                return transcript;
            }
            return "";
        }

        [HttpGet("gettranscript")]
        [Authorize]
        public List<string> GetTranscriptHistory()
        {
            foreach (var transcript in _audioListener.GetTranscriptHistory())
            {
                Console.WriteLine(transcript);
            }
            return _audioListener.GetTranscriptHistory();
        }

        [HttpPost("start/{appointmentId}")]
        [Authorize]
        public IActionResult Start(int appointmentId)
        {
            _audioListener.StartListening(appointmentId);
            return Ok("Listening started");
        }

        [HttpPost("stop/{appointmentId}")]
        [Authorize]
        public IActionResult Stop(int appointmentId)
        {
            _audioListener.StopListening(appointmentId);
            return Ok("Listening stopped");
        }

    }
}

