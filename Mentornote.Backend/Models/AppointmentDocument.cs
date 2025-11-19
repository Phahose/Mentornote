#nullable disable
namespace Mentornote.Backend.Models
{
    public class AppointmentDocument
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AppointmentId { get; set; }
        public string DocumentPath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Appointment Appointment { get; set; }
        public string FileHash { get; set; }
    }
}

