# nullable disable
using Mentornote.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;

namespace Mentornote.Pages
{
    public class SignUpModel : PageModel
    {
        [BindProperty]
        public string FirstName { get; set; } = string.Empty;
        [BindProperty]
        public string LastName { get; set; } = string.Empty;
        [BindProperty]
        public string Email { get; set; } = string.Empty;
        [BindProperty]
        public string Password { get; set; } = string.Empty;
        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;
        public List<string> ErrorList { get; set; } = new List<string>();   
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        private readonly AuthController _authController;
        public SignUpModel(AuthController authController)
        {
            _authController = authController;
        }
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (FirstName.IsNullOrEmpty())
            {
                ErrorMessage= "First name is required.";
                ErrorList.Add(ErrorMessage);
            }
            if (LastName.IsNullOrEmpty())
            {
                ErrorMessage = "Last name is required";
                ErrorList.Add(ErrorMessage);
            }
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
            if (ConfirmPassword.IsNullOrEmpty())
            {
                ErrorMessage = "Must Confirm password";
                ErrorList.Add(ErrorMessage);
            }   
            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                ErrorList.Add(ErrorMessage);
            }


            if (ErrorList.Count == 0)
            {
                _authController.Register(new Mentornote.DTOs.UserDto
                {
                    FirstName = FirstName,
                    LastName = LastName,
                    Email = Email,
                    Password = Password
                }).Wait();
                SuccessMessage = "User registered successfully.";
            }
            return RedirectToPage("/Login");
        }
    }
}
