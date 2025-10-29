using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mentornote.Desktop.MVVM
{
    public class Helper
    {
        public async Task<string> GetFullTranscriptAsync()
        {
            try
            {
                using var http = new HttpClient();
                var response = await http.GetAsync("http://localhost:5085/api/transcribe/context");
                response.EnsureSuccessStatusCode();

                var transcript = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Retrieved transcript from backend: {transcript}");
                return transcript;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving transcript: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
