namespace Mentornote.Backend.Models
{
    public class Utterance
    {
        public int Speaker { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public string Text { get; set; }
        public int AppointmentId { get; set; }
    }
}
