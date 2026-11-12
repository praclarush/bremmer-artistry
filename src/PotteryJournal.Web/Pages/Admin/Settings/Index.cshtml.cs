using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Settings
{
    public class IndexModel : PageModel
    {
        private readonly IAdminSettingsHandler _adminSettingsHandler;

        public IndexModel(IAdminSettingsHandler adminSettingsHandler)
        {
            _adminSettingsHandler = adminSettingsHandler;
        }

        [BindProperty]
        public AdminSettingsModel Settings { get; set; } = new AdminSettingsModel();

        public async Task OnGetAsync()
        {
            DataHandlerResponse<AdminSettingsModel> response = await _adminSettingsHandler.GetAsync();
            if (response.IsSuccess && response.Data is not null)
            {
                Settings = response.Data;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            HandlerResponse response = await _adminSettingsHandler.UpdateAsync(Settings);
            TempData["StatusMessage"] = response.IsSuccess
                ? "Settings saved."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }
    }
}
