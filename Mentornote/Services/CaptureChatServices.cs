using Mentornote.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Mentornote.Services
{
    public class CaptureChatServices
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Helpers _helpers;

        public CaptureChatServices(HttpClient httpClient, IConfiguration config, Helpers helpers)
        {
            _httpClient = httpClient;
            _config = config;
            _helpers = helpers;
        }

    }
}
