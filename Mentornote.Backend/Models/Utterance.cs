namespace Mentornote.Backend.Models
{
    public class Utterance
    {
        public int Speaker { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Text { get; set; }
        public int AppointmentId { get; set; }
    }
}
