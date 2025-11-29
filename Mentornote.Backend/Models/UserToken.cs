namespace Mentornote.Backend.Models
{
    public class UserToken
    {
        public int UserId { get; set; } 
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }

    }
}
