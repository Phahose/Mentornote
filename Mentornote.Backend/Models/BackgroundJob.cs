using System.ComponentModel.DataAnnotations;

namespace Mentornote.Backend.Models
{
    public class BackgroundJob
    {
        public long Id { get; set; } 
        public string JobType { get; set; } = null!;
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Payload { get; set; }
        public string? ResultMessage { get; set; }
        public string? ErrorTrace { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

    }
}
