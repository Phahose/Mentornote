#nullable disable
using Mentornote.Backend.DTO;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly DBServices _dbService;

        public AuthController(AuthService authService, DBServices dBServices)
        {
            _authService = authService;
            _dbService = dBServices;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var result = _authService.AuthenticateAsync(dto.Email, dto.Password);

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

            if (existingToken.CreatedAt < user.PasswordChangedAt)
            {
                return Unauthorized("Session expired. Please log in again.");
            }

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

        [Authorize]
        [HttpGet("getUser")]
        public IActionResult GetUserByEmail()
        {
            string email = (string)(User.FindFirst(ClaimTypes.Email).Value);
            var user = _dbService.GetUserByEmail(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            return Ok(user);

        }

        [Authorize]
        [HttpPost("consume-trial")]
        public async Task<IActionResult> ConsumeTrial()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            await _dbService.UpdateUserTrialAfterMeetingAsync(userId);

            return Ok();
        }

        [Authorize]
        [HttpPost("change-password")]
        public IActionResult ChangePassword(ChangePasswordDto dto)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var result = _authService.ChangePassword(userId, dto);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok(result.Message);
        }

       

    }
}
