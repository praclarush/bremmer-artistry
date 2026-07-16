using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.AllowedAdmins
{
    public class IndexModel : PageModel
    {
        private readonly IAllowedAdminsHandler _allowedAdminsHandler;

        public IndexModel(IAllowedAdminsHandler allowedAdminsHandler)
        {
            _allowedAdminsHandler = allowedAdminsHandler;
        }

        public List<AllowedAdminModel> Admins { get; private set; } = new List<AllowedAdminModel>();

        [BindProperty]
        public string NewEmail { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string? NewDisplayName { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAdminsAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (string.IsNullOrWhiteSpace(NewEmail) || string.IsNullOrWhiteSpace(NewPassword))
            {
                TempData["StatusMessage"] = "An email address and password are required.";
                return RedirectToPage();
            }

            string addedByEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            DataHandlerResponse<Guid> response = await _allowedAdminsHandler.AddAsync(NewEmail, NewPassword, NewDisplayName, addedByEmail);

            TempData["StatusMessage"] = response.IsSuccess
                ? $"{NewEmail} was added as an admin."
                : string.Join(" ", response.Errors);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(Guid id)
        {
            HandlerResponse response = await _allowedAdminsHandler.RemoveAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "The allow-list entry was removed."
                : string.Join(" ", response.Errors);

            return RedirectToPage();
        }

        private async Task LoadAdminsAsync()
        {
            DataHandlerResponse<List<AllowedAdminModel>> response = await _allowedAdminsHandler.GetAllAsync();
            if (response.IsSuccess && response.Data is not null)
            {
                Admins = response.Data;
            }
        }
    }
}
