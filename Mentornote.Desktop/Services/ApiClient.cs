using System.Net.Http;
using System.Net.Http.Headers;

namespace Mentornote.Desktop.Services
{
    public static class ApiClient
    {
        public static HttpClient Client { get; private set; }

        static ApiClient()
        {
            Client = new HttpClient();
            Client.BaseAddress = new Uri("http://localhost:5085/api/");
        }

        public static void SetToken(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
