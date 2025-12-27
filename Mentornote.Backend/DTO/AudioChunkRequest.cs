namespace Mentornote.Backend.DTO
{
    public class AudioChunkRequest
    {
        public int AppointmentId { get; set; }
        public byte[] WavChunk { get; set; }
    }
}
