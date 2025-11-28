using Mentornote.Backend.DTO;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var result = await _authService.AuthenticateAsync(dto.Email, dto.Password);

            if (result == null)
            {
                return Unauthorized("Invalid email or password.");
            }
                

            return Ok(result);
        }
    }
}
