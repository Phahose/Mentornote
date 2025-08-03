#nullable disable
using Mentornote.Data;
using Mentornote.DTOs;
using Mentornote.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace Mentornote.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(ApplicationDbContext context, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDto request)
        {
            // ✅ Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required.");

            if (!request.Email.Contains("@") || request.Password.Length < 6)
                return BadRequest("Invalid email or password too short.");

            // ✅ Check for existing user
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("User already exists.");

            // ✅ Hash password
            using var hmac = new HMACSHA512();
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password)),
                PasswordSalt = hmac.Key,
                AuthProvider = "local"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        /*   [HttpPost("login")]
           public async Task<IActionResult> Login(UserDto request)
           {
               var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
               if (user == null)
                   return BadRequest("User not found.");

               if (user.AuthProvider != "local")
                   return BadRequest($"Please log in using {user.AuthProvider}.");

               using var hmac = new HMACSHA512(user.PasswordSalt);
               var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password));

               for (int i = 0; i < computedHash.Length; i++)
               {
                   if (computedHash[i] != user.PasswordHash[i])
                       return BadRequest("Incorrect password.");
               }
               var token = CreateJwtToken(user);
               return Ok(new { Token = token });
           }*/
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserDto request)
        {
            var context = _httpContextAccessor.HttpContext;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return BadRequest("User not found.");

            if (user.AuthProvider != "local")
                return BadRequest($"Please log in using {user.AuthProvider}.");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                    return BadRequest("Incorrect password.");
            }

            // Create Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Remember login across sessions
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Sign in with cookie
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return Ok(new { message = "Login successful" });
        }


        [NonAction]
        private string CreateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
             };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiresInMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
