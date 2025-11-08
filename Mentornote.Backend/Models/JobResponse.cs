namespace Mentornote.Backend.Models
{
    public class JobResponse
    {
        public long jobId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ResultMessage { get; set; }
    }
}
