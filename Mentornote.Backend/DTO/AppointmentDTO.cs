#nullable disable
namespace Mentornote.Backend.DTO
{
    public class AppointmentDTO
    {
        // Appointment Info
        public string Title { get; set; }
        public string Organizer { get; set; }
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }  

        //Document Info
        public List<IFormFile> Files { get; set; }
        public int UserId { get; set; }
    }
}
