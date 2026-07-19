using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages
{
    [EnableRateLimiting(RateLimiterPolicies.ClassBooking)]
    public class ClassesModel : PageModel
    {
        private readonly IClassesHandler _classesHandler;

        public ClassesModel(IClassesHandler classesHandler)
        {
            _classesHandler = classesHandler;
        }

        [BindProperty]
        public ClassBookingSaveModel Booking { get; set; } = new ClassBookingSaveModel();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostBookAsync()
        {
            DataHandlerResponse<Guid> response = await _classesHandler.CreateBookingAsync(Booking);
            if (!response.IsSuccess)
            {
                TempData["StatusMessage"] = string.Join(" ", response.Errors);
                return RedirectToPage();
            }

            TempData["StatusMessage"] = response.Warnings.Count > 0
                ? "Thanks! Your request is tentative until we confirm it. " + string.Join(" ", response.Warnings)
                : "Thanks! Your request is tentative until we confirm it -- you'll get an email once it's approved.";
            return RedirectToPage();
        }
    }
}
