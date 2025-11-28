using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mentornote.Backend.Services
{
    public class AuthService
    {
        private readonly DBServices _dbServices = new DBServices();
        private readonly IConfiguration _config;

        public AuthService( IConfiguration config)
        {
            _config = config;
        }

        public async Task<LoginResponseDTO> AuthenticateAsync(string email, string password)
        {
           // var user = await _dbServices.GetUserByEmail(u => u.Email == email);


            var user = new User(); // Placeholder for user retrieval logic
            if (user == null)
            {
                return null;
            }

            // verify password hash
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            if (!computedHash.SequenceEqual(user.PasswordHash))
                return null;

            string token = GenerateJwtToken(user);

            return new LoginResponseDTO
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                ExpiresAt = DateTime.UtcNow.AddHours(12)
            };
        }

        //public async Task<bool> RegisterAsync(RegisterRequestDto dto)
        //{
        //    if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
        //        return false;

        //    using var hmac = new HMACSHA512();

        //    var user = new User
        //    {
        //        Email = dto.Email,
        //        PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)),
        //        PasswordSalt = hmac.Key,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    _db.Users.Add(user);
        //    await _db.SaveChangesAsync();

        //    return true;
        //}

        private string GenerateJwtToken(User user)
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
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
