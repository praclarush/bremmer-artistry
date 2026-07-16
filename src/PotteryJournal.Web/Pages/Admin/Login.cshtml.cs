using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PotteryJournal.Web.Pages.Admin
{
    public class LoginModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Admin/Index");
            }

            return Page();
        }

        public IActionResult OnPostChallenge()
        {
            AuthenticationProperties properties = new AuthenticationProperties
            {
                RedirectUri = "/Admin",
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
    }
}
