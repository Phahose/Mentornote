using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Mentornote.Desktop.Models;

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
                return false;

            var requestBody = new
            {
                RefreshToken = RefreshToken
            };

            var response = await Client.PostAsJsonAsync("auth/refresh", requestBody);

            if (!response.IsSuccessStatusCode)
                return false;

            var tokenResult = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResult == null)
                return false;

            // Apply the new tokens
            SetToken(tokenResult.AccessToken, tokenResult.RefreshToken);

            return true;
        }
    }
}
