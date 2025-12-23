#nullable disable
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : Controller
    {
        DBServices _dBServices;

        public SettingsController(DBServices dBServices)
        {
            _dBServices = dBServices;
        }

        [HttpGet("getAppsettings")]
        [Authorize]
        public async Task<ActionResult<AppSettings>> Get()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var settings = await _dBServices.GetAsync(userId);
            return Ok(settings);
        }

        [HttpPut("savesettings")]
        [Authorize]
        public async Task<IActionResult> Save([FromBody] AppSettings settings)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            await _dBServices.SaveAsync(userId, settings);
            return NoContent();
        }

    }
}
