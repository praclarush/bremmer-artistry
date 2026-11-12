using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Classes.Blackouts
{
    public class IndexModel : PageModel
    {
        private readonly IClassesHandler _classesHandler;

        public IndexModel(IClassesHandler classesHandler)
        {
            _classesHandler = classesHandler;
        }

        public List<BlackoutPeriodModel> BlackoutPeriods { get; private set; } = new List<BlackoutPeriodModel>();

        [BindProperty]
        public BlackoutPeriodSaveModel NewBlackoutPeriod { get; set; } = new BlackoutPeriodSaveModel();

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            DataHandlerResponse<Guid> response = await _classesHandler.AddBlackoutPeriodAsync(NewBlackoutPeriod);
            TempData["StatusMessage"] = response.IsSuccess
                ? "Blackout period added."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(Guid id)
        {
            HandlerResponse response = await _classesHandler.RemoveBlackoutPeriodAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "Blackout period removed."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            DataHandlerResponse<List<BlackoutPeriodModel>> response = await _classesHandler.GetBlackoutPeriodsAsync();
            if (response.IsSuccess && response.Data is not null)
            {
                BlackoutPeriods = response.Data;
            }
        }
    }
}
