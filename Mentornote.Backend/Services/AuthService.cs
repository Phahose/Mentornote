#nullable disable
using Azure.Core;
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
        private readonly DBServices _dBServices = new DBServices();

        public AuthService( IConfiguration config, DBServices dBServices)
        {
            _config = config;
            _dBServices = dBServices;
        }

        public LoginResponseDTO AuthenticateAsync(string email, string password)
        {
            var user =  _dbServices.GetUserByEmail(email);

            if (user == null)
            {
                return null;
            }

            // verify password hash
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            if (!computedHash.SequenceEqual(user.PasswordHash))
            {
                return null;
            }

            string accesstoken = GenerateJwtToken(user);

            var refreshToken = CreateRefreshToken(user);
            
            _dbServices.SaveRefreshToken(refreshToken);

            return new LoginResponseDTO
            {
                Token = accesstoken,
                UserId = user.Id,
                RefreshToken = refreshToken.Token,
                Email = user.Email,
                ExpiresAt = DateTime.UtcNow.AddHours(12)
            };
        }

        public (bool Success ,string Message) RegisterAsync(UserDto dto)
        {
            var existingUser = _dBServices.GetUserByEmail(dto.Email);

            if (existingUser.Email != "")
            {
                return (false, "This User Email Already Exists");
            }


            using var hmac = new HMACSHA512();
            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)),
                PasswordSalt = hmac.Key,
                AuthProvider = "local",
                CreatedAt = DateTime.UtcNow,
                UserType = dto.UserType
            };

            int userId = _dBServices.RegisterUser(user);

            return (true, "Account Created Successfully");
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName),
                new Claim("fullName", $"{user.FirstName} {user.LastName}"),
                new Claim("userType", user.UserType), 
                new Claim("createdAt", user.CreatedAt.ToString()) 
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

        public RefreshToken CreateRefreshToken(User user)
        {
            return new RefreshToken
            {
                UserId = user.Id,
                Email = user.Email,   
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(120),
                CreatedAt = DateTime.UtcNow
            };
        }

    }
}
