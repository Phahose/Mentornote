using Mentornote.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Mentornote.Services
{
    public class Helpers
    {
        public string GenerateRandomCode(int length)
        {
            const string chars = "0123456789"; // Use only numbers
            var random = new Random();


            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
