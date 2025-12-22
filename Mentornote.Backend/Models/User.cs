#nullable disable
namespace Mentornote.Backend.Models
{
    public class User
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string AuthProvider { get; set; } = "local";
        public DateTime CreatedAt { get; set; }
        public string UserType { get; set; }
        public int TrialMeetingsRemaining { get; set; }
        public bool IsSubscribed { get; set; }
        public DateTime PasswordChangedAt { get; set; }
    }
}
