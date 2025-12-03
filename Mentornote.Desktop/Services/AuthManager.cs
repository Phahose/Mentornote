#nullable disable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mentornote.Desktop.Models;

namespace Mentornote.Desktop.Services
{
    public static class AuthManager
    {
        private static string TokenFilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MentorNote", "auth.dat");

        static AuthManager()
        {
            var dir = Path.GetDirectoryName(TokenFilePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static void SaveTokens(string accessToken, string refreshToken)
        {
            var data = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);

            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(TokenFilePath, encrypted);
        }

        public static TokenResponse LoadTokens()
        {
            if (!File.Exists(TokenFilePath))
                return null;

            try
            {
                var encrypted = File.ReadAllBytes(TokenFilePath);
                var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);

                var json = Encoding.UTF8.GetString(decrypted);
                return JsonSerializer.Deserialize<TokenResponse>(json);
            }
            catch
            {
                return null;
            }
        }

        public static bool IsLoggedIn()
        {
            var tokens = LoadTokens();
            return tokens != null &&
                   !string.IsNullOrWhiteSpace(tokens.AccessToken) &&
                   !string.IsNullOrWhiteSpace(tokens.RefreshToken);
        }

        public static void Logout()
        {
            if (File.Exists(TokenFilePath))
                File.Delete(TokenFilePath);
        }
    }
}
