using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mentornote.Pages.Functionalities
{
    public class Ask_TutorModel : PageModel
    {
        public string Messages { get; set; } = "Your application description page.";
        public void OnGet()
        {
        }
    }
}
