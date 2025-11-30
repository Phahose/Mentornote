using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/summary")]
    public class SummaryController : Controller
    {
        DBServices dBServices = new DBServices();
       
        [HttpPost("save/{appointmentId}")]
        [Authorize]
        public IActionResult SaveSummary(int appointmentId, [FromBody] SummaryRequest request)
        {
            try
            {
                int id = dBServices.AddAppointmentSummary(appointmentId, request.Summary);
                return Ok(new { SummaryId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Failed to save summary: {ex.Message}");
            }
        }

        [HttpGet("getsummary/{appointmentId}")]
        [Authorize]
        public IActionResult GetSummary(int appointmentId)
        {
            var summary = dBServices.GetSummaryByAppointmentId(appointmentId);

            if (summary == null)
                return NotFound("No summary exists for this appointment.");

            return Ok(summary);
        }

    }

    
}
