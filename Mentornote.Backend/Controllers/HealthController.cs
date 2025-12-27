using Microsoft.AspNetCore.Mvc;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Healthy");
    }

}
