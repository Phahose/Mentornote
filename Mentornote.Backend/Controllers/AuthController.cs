using DocumentFormat.OpenXml.InkML;
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.Data;
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
        private readonly DBServices _dbService = new DBServices();

        public AuthController(AuthService authService)
        {
            _authService = authService;
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

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {
            var existingToken = _dbService.GetRefreshToken(request.RefreshToken);

            if (existingToken == null || !existingToken.IsActive)
            {
                return Unauthorized("Invalid or expired refresh token.");
            }
            // Rotate refresh token
            var user = _dbService.GetUserByEmail(existingToken.Email);

            var newRefreshToken = _authService.CreateRefreshToken(user);

            _dbService.RevokeToken(existingToken.Id, newRefreshToken.Token);
            _dbService.DeleteRefreshToken(existingToken.Id);
            _dbService.SaveRefreshToken(newRefreshToken);

           
            var newAccessToken = _authService.GenerateJwtToken(user);

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken.Token,
                expiresIn = 3600
            });
        }

    }
}
