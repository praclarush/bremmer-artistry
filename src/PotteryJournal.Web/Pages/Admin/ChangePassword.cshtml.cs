using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin
{
    public class ChangePasswordModel : PageModel
    {
        private readonly IAllowedAdminsHandler _allowedAdminsHandler;

        public ChangePasswordModel(IAllowedAdminsHandler allowedAdminsHandler)
        {
            _allowedAdminsHandler = allowedAdminsHandler;
        }

        [BindProperty]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmNewPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; private set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (NewPassword != ConfirmNewPassword)
            {
                ErrorMessage = "New password and confirmation do not match.";
                return Page();
            }

            string email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            DataHandlerResponse<AllowedAdminModel> verifyResponse = await _allowedAdminsHandler.ValidateCredentialsAsync(email, CurrentPassword);
            if (!verifyResponse.IsSuccess || verifyResponse.Data is null)
            {
                ErrorMessage = "Current password is incorrect.";
                return Page();
            }

            HandlerResponse response = await _allowedAdminsHandler.ChangePasswordAsync(verifyResponse.Data.Id, NewPassword);
            TempData["StatusMessage"] = response.IsSuccess
                ? "Password updated."
                : string.Join(" ", response.Errors);

            return RedirectToPage();
        }
    }
}
