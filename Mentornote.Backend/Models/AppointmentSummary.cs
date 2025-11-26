namespace Mentornote.Backend.Models
{
    public class AppointmentSummary
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public string SummaryText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

    }
}
