using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin
{
    [EnableRateLimiting(RateLimiterPolicies.LoginAttempts)]
    public class LoginModel : PageModel
    {
        private readonly IAllowedAdminsHandler _allowedAdminsHandler;

        public LoginModel(IAllowedAdminsHandler allowedAdminsHandler)
        {
            _allowedAdminsHandler = allowedAdminsHandler;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; private set; }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Admin/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            DataHandlerResponse<AllowedAdminModel> response = await _allowedAdminsHandler.ValidateCredentialsAsync(Email, Password);
            if (!response.IsSuccess || response.Data is null)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            AllowedAdminModel admin = response.Data;
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Name, admin.DisplayName ?? admin.Email),
            };
            ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            return RedirectToPage("/Admin/Index");
        }
    }
}
