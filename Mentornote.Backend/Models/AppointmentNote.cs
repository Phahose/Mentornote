﻿#nullable disable
namespace Mentornote.Backend.Models
{
    public class AppointmentNote
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AppointmentId { get; set; }
        public string DocumentPath { get; set; }
        public string? Chunk { get; set; }
        public string? Vector { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Appointment? Appointment { get; set; }
    }
}
