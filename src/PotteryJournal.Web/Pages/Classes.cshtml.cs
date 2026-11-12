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
                // Land back on the class type the visitor was already looking at (e.g. the slot
                // they picked just got booked by someone else) instead of the bare picker -- losing
                // the class-type selection on top of the failed booking is unnecessary friction.
                return RedirectToPage(pageName: null, pageHandler: null, routeValues: null, fragment: $"type/{Booking.ClassTypeId}");
            }

            string formattedWhen = Booking.StartDateTime.ToString("dddd, MMMM d 'at' h:mm tt");
            TempData["StatusMessage"] = response.Warnings.Count > 0
                ? $"Thanks! Your request for {formattedWhen} is in -- tentative until we confirm it. " + string.Join(" ", response.Warnings)
                : $"Thanks! Your request for {formattedWhen} is in -- tentative until we confirm it by email.";
            return RedirectToPage();
        }
    }
}
