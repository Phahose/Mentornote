using Mentornote.Desktop.Helpers;
using Mentornote.Backend.Models;

namespace Mentornote.Desktop.Services
{
    public static class UserSession
    {
        public static UserToken CurrentUser { get; private set; } 

        public static void SetUser(string token)
        {
            var user = JwtHelper.DecodeToken(token);
            CurrentUser = user;
        }

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}
