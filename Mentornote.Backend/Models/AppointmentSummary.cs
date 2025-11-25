namespace Mentornote.Backend.Models
{
    public class AppointmentSummary
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public string SummaryText { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
