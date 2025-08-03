#nullable disable
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mentornote.Controllers;
using Microsoft.IdentityModel.Tokens;
using Mentornote.DTOs;
using System.Threading.Tasks;

namespace Mentornote.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; } = string.Empty;
        [BindProperty]
        public string Password { get; set; } = string.Empty;
        public List<string> ErrorList { get; set; } = new List<string>();
        public string ErrorMessage { get; set; } = string.Empty;
        private readonly AuthController _authController;

        public LoginModel(AuthController authController)
        {
            _authController = authController;
        }
        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPost()
        {
            if (Email.IsNullOrEmpty())
            {
                ErrorMessage = "Email is required.";
                ErrorList.Add(ErrorMessage);
            }
            if (Password.IsNullOrEmpty())
            {
                ErrorMessage = "Password is required.";
                ErrorList.Add(ErrorMessage);
            }
            if (ErrorList.Count > 0)
            {
                return Page();
            }
            else
            {
                UserDto user = new()
                {
                    Email = Email,
                    Password = Password
                };
                var result = await _authController.Login(user); 
                if (result is OkObjectResult okResult)
                {
                    HttpContext.Session.SetString("Email", Email);
                    return RedirectToPage("/Start");
                }
                else if (result is BadRequestObjectResult badRequest)
                {
                    ErrorMessage = "Invalid login attempt.";
                    ErrorList.Add(ErrorMessage);
                    return Page();
                }
            }
            return Page();
        }
    }
}
