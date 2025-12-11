#nullable disable
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
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
        private readonly GeminiServices _geminiServices;

        public TranscribeController(ConversationMemory memory, Transcribe transcribe, AudioListener audioListener, GeminiServices geminiServices)
        {
            _memory = memory;
            _transcribe = transcribe;
            _audioListener = audioListener;
            _geminiServices = geminiServices;
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
                return transcript;
            }
            return "";
        }

        [HttpGet("gettranscript")]
        [Authorize]
        public List<Utterance> GetTranscriptHistory()
        {
            var transcripts = _audioListener.GetTranscriptHistory();
            transcripts = transcripts.Where(u => u != null).ToList();
           
            return transcripts;
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
        public async Task<IActionResult> Stop(int appointmentId)
        {
            _audioListener.StopListening(appointmentId);

            return Ok("Listening stopped");
        }

        [HttpPost("pause")]
        [Authorize]
        public async Task<IActionResult> Pause()
        {
            _audioListener.PauseListening();

            return Ok("Listening paused");
        }

        
        [HttpPost("resume")]
        [Authorize]
        public IActionResult Resune()
        {
            _audioListener.ResumeListening();

            return Ok("Listening Resumed");
        }

        
        [HttpGet("memory")]
        public ActionResult<List<string>> GetMemory()
        {
            var summaries = _geminiServices.GetSummaries() ?? new List<string>();
            return Ok(summaries);
        }
    }
}

