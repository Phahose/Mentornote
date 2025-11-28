using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Mentornote.Desktop.Services
{
    public static class AuthManager
    {
        private static string TokenFilePath =>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MentorNote", "auth.dat");

        static AuthManager()
        {
            var dir = Path.GetDirectoryName(TokenFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static void SaveToken(string token)
        {
            var data = Encoding.UTF8.GetBytes(token);

            var encrypted = ProtectedData.Protect(
                data,
                null,
                DataProtectionScope.CurrentUser
            );

            File.WriteAllBytes(TokenFilePath, encrypted);
        }

        public static string LoadToken()
        {
            if (!File.Exists(TokenFilePath))
                return null;

            try
            {
                var encrypted = File.ReadAllBytes(TokenFilePath);

                var decrypted = ProtectedData.Unprotect(
                    encrypted,
                    null,
                    DataProtectionScope.CurrentUser
                );

                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return null;
            }
        }

        public static bool IsLoggedIn()
        {
            var token = LoadToken();
            return !string.IsNullOrEmpty(token);
            // Optionally: check JWT expiry here
        }

        public static void Logout()
        {
            if (File.Exists(TokenFilePath))
            {
                File.Delete(TokenFilePath);
            }
        }
    }
}
