#nullable disable
using Azure.Core;
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


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
                PasswordChangedAt = DateTime.UtcNow,
                UserType = dto.UserType,
                TrialMeetingsRemaining = dto.TrialMeetingsRemaining,
                IsSubscribed = dto.IsSubscribed
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
                new Claim("createdAt", user.CreatedAt.ToString()),
                new Claim("pwd_changed_at", user.PasswordChangedAt.ToString("O"))
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

        public (bool Success, string Message) ChangePassword(int userId, ChangePasswordDto dto)
        {
            var user = _dBServices.GetUserById(userId);

            if (user == null)
            {
                return (false, "User not found.");
            }

            // 1. Verify current password
            bool validPassword = VerifyPassword( dto.CurrentPassword,user.PasswordHash, user.PasswordSalt);

            if (!validPassword)
            {
                return (false, "Current password is incorrect.");
            }

            // 2. Prevent reuse
            if (dto.CurrentPassword == dto.NewPassword)
            {
                return (false, "New password must be different.");
            }
                
            // 3. Hash new password (NEW SALT)
            using var hmac = new HMACSHA512();

            user.PasswordHash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(dto.NewPassword)
            );

            user.PasswordSalt = hmac.Key;
            user.PasswordChangedAt = DateTime.UtcNow;

            _dBServices.UpdateUserPassword(user);

            return (true, "Password updated successfully.");
        }


        private bool VerifyPassword(
                            string inputPassword,
                            byte[] storedHash,
                            byte[] storedSalt)
        {
            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(inputPassword)
            );

            return computedHash.SequenceEqual(storedHash);
        }


    }
}
