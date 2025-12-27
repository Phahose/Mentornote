#nullable disable
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Mentornote.Desktop.Models;
using Mentornote.Desktop.Windows;


namespace Mentornote.Desktop.Services
{
    public static class ApiClient
    {
        public static HttpClient Client { get; private set; }
        public static string AccessToken { get; private set; }
        public static string RefreshToken { get; private set; }

        static ApiClient()
        {
            Client = new HttpClient();
            Client.BaseAddress = new Uri("http://localhost:5085/api/");
            //Client.BaseAddress = new Uri("https://api.mentornote.app/api/");
        }

        public static void SetToken(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // Call backend to refresh access token
        public static async Task<bool> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                return false;
            }           

            var requestBody = new
            {
                RefreshToken = RefreshToken
            };

            var response = await Client.PostAsJsonAsync("auth/refresh", requestBody);


            if (!response.IsSuccessStatusCode)
            {
                // Refresh token dead → full logout required
                AuthManager.Logout();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Redirect user to login window
                    var login = new AuthWindow();
                    login.Show();

                    // Close the main window
                    System.Windows.Application.Current.MainWindow.Close();
                });

                return false;
            }


            var tokenResult = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResult == null)
            {
                return false;
            }
            // Apply the new tokens
            AuthManager.SaveTokens(tokenResult.AccessToken, tokenResult.RefreshToken);
            SetToken(tokenResult.AccessToken, tokenResult.RefreshToken);
           
            return true;
        }
    }
}
