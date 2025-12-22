#nullable disable
namespace Mentornote.Backend.DTO
{
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
