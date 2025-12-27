#nullable disable
using Mentornote.Backend.DTO;
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

        private readonly GeminiServices _geminiServices;

        public TranscribeController(ConversationMemory memory, Transcribe transcribe, GeminiServices geminiServices)
        {
            _memory = memory;
            _transcribe = transcribe;

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

        [HttpGet("memory")]
        [Authorize]
        public ActionResult<List<string>> GetMemory()
        {
            var summaries = _geminiServices.GetSummaries() ?? new List<string>();
            return Ok(summaries);
        }

        [HttpPost("deepgramTranscribe/live")]
        [Authorize]
        public async Task<IActionResult> DeepgramLive(AudioChunkRequest request)
        {
            if (request.WavChunk == null || request.WavChunk.Length == 0)
            {
                return BadRequest("Empty audio Chunk");
            }
            var transcriptList = await _transcribe.DeepGramLiveTranscribe(request.WavChunk, request.AppointmentId);

            return Ok(transcriptList);
        }

    }
}

