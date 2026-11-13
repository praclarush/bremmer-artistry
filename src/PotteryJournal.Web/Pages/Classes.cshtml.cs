using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages
{
    [EnableRateLimiting(RateLimiterPolicies.ClassBooking)]
    public class ClassesModel : PageModel
    {
        private readonly IClassesHandler _classesHandler;
        private readonly SiteFeaturesOptions _siteFeatures;

        public ClassesModel(IClassesHandler classesHandler, IOptions<SiteFeaturesOptions> siteFeatures)
        {
            _classesHandler = classesHandler;
            _siteFeatures = siteFeatures.Value;
        }

        [BindProperty]
        public ClassBookingSaveModel Booking { get; set; } = new ClassBookingSaveModel();

        public IActionResult OnGet()
        {
            return _siteFeatures.ShowClasses ? Page() : NotFound();
        }

        public async Task<IActionResult> OnPostBookAsync()
        {
            if (!_siteFeatures.ShowClasses)
            {
                return NotFound();
            }

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
