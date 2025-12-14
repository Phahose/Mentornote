using Mentornote.Backend.Models;

namespace Mentornote.Backend.DTO
{
    public class SuggestionRequest
    {
        public string UserQuestion { get; set; }
        public List<string> RecentUtterances { get; set; }
        public List<string> MemorySummaries { get; set; }
        public AppSettings AppSettings { get; set; }
    }
}
