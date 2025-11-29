using DocumentFormat.OpenXml.InkML;
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly DBServices _dBServices = new DBServices();

        public AuthController(AuthService authService, DBServices dBServices)
        {
            _authService = authService;
            _dBServices = dBServices;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var result =  _authService.AuthenticateAsync(dto.Email, dto.Password);

            if (result == null)
            {
                return Unauthorized("Invalid email or password.");
            }
                

            return Ok(result);
        }

        [HttpPost("register")]
        public IActionResult Register(UserDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var result = _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return BadRequest($"Could Not add {result.Message}");
            }

            return Ok($"User registered successfully.");
        }
    }
}
