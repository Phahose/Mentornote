namespace Mentornote.Backend.DTO
{
    public class AppointmentFileUploadDTO
    {
        public IFormFile File { get; set; }
        public int AppointmentId { get; set; }
        public int UserId { get; set; }
    }
}
