#nullable disable
using Azure;
using Mentornote.Controllers;
using Mentornote.DTOs;
using Mentornote.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text;

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

                return await Login(user);
            }
        }

        public async Task<IActionResult> Login(UserDto request)
        {

            UsersService usersService = new();
            var user = usersService.GetUserByEmail(Email);
            if (user == null)
                return BadRequest("User not found.");

            if (user.AuthProvider != "local")
                return BadRequest($"Please log in using {user.AuthProvider}.");


            // Convert user Input to byte[] hash and then to strings
            using var hmac = new HMACSHA512(user.PasswordSalt);

            byte[] enteredHashedPassword = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password));


            if (ByteArraysAreEqual(user.PasswordHash, enteredHashedPassword))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                AuthenticationProperties authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Remember login across sessions
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                };

                // Sign in with cookie
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                HttpContext.Session.SetString("Email", Email);
                return RedirectToPage("/Start");
            }

            return Page();

        }

        private bool ByteArraysAreEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }
    }
}
