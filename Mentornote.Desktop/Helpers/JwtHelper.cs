#nullable disable
using Mentornote.Backend.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace Mentornote.Desktop.Helpers
{
    public static class JwtHelper
    {
        public static UserToken DecodeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }
                

            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
            {
                return null;
            }

            var jwt = handler.ReadJwtToken(token);

            var model = new UserToken
            {
                UserId = TryGetInt(jwt, ClaimTypes.NameIdentifier),
                Email = TryGet(jwt, ClaimTypes.Email),
                FirstName = TryGet(jwt, "firstName"),
                LastName = TryGet(jwt, "lastName"),
                FullName = TryGet(jwt, "fullName"),

                UserType = TryGet(jwt, "userType"),
                CreatedAt = TryGetDate(jwt, "createdAt")
            };

            return model;
        }

        private static string TryGet(JwtSecurityToken token, string claimType)
        {
            return token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }

        private static int TryGetInt(JwtSecurityToken token, string claimType)
        {
            var value = TryGet(token, claimType);
            return int.TryParse(value, out int result) ? result : 0;
        }

        private static DateTime? TryGetDate(JwtSecurityToken token, string claimType)
        {
            var value = TryGet(token, claimType);
            return DateTime.TryParse(value, out DateTime result) ? result : null;
        }
    }
}
