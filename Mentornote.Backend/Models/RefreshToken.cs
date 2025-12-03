namespace Mentornote.Backend.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;

        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string ReplacedByToken { get; set; }

        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
