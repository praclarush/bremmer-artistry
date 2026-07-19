using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Classes.Bookings
{
    public class IndexModel : PageModel
    {
        private readonly IClassesHandler _classesHandler;

        public IndexModel(IClassesHandler classesHandler)
        {
            _classesHandler = classesHandler;
        }

        [BindProperty(SupportsGet = true)]
        public ClassBookingStatus? Status { get; set; }

        public List<ClassBookingModel> Bookings { get; private set; } = new List<ClassBookingModel>();

        public async Task OnGetAsync()
        {
            DataHandlerResponse<List<ClassBookingModel>> response = await _classesHandler.GetBookingsAsync(Status);
            if (response.IsSuccess && response.Data is not null)
            {
                Bookings = response.Data;
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid id)
        {
            HandlerResponse response = await _classesHandler.ApproveBookingAsync(id);
            if (!response.IsSuccess)
            {
                TempData["StatusMessage"] = string.Join(" ", response.Errors);
            }
            else if (response.Warnings.Count > 0)
            {
                TempData["StatusMessage"] = "Booking approved. " + string.Join(" ", response.Warnings);
            }
            else
            {
                TempData["StatusMessage"] = "Booking approved.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeclineAsync(Guid id)
        {
            HandlerResponse response = await _classesHandler.DeclineBookingAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "Booking declined."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }
    }
}
