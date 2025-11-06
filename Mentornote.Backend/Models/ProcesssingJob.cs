namespace Mentornote.Backend.Models
{
    public class ProcesssingJob
    {
        public string JobId { get; set; } = Guid.NewGuid().ToString();
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
        public string? ResultMessage { get; set; }
    }
}
