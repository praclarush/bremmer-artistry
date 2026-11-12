using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Calendar
{
    public class IndexModel : PageModel
    {
        private readonly IEventsHandler _eventsHandler;
        private readonly IClassesHandler _classesHandler;

        public IndexModel(IEventsHandler eventsHandler, IClassesHandler classesHandler)
        {
            _eventsHandler = eventsHandler;
            _classesHandler = classesHandler;
        }

        public void OnGet()
        {
        }

        public async Task<JsonResult> OnGetDataAsync(DateTimeOffset start, DateTimeOffset end)
        {
            DataHandlerResponse<List<EventModel>> eventsResponse = await _eventsHandler.GetOccurrencesInRangeAsync(start, end);
            DataHandlerResponse<List<ClassBookingModel>> bookingsResponse = await _classesHandler.GetBookingsInRangeAsync(start, end);

            List<CalendarItemModel> items = new List<CalendarItemModel>();

            foreach (EventModel evt in eventsResponse.Data ?? new List<EventModel>())
            {
                items.Add(new CalendarItemModel
                {
                    Type = "Event",
                    Title = evt.Title,
                    StartDateTime = evt.StartDateTime,
                    EndDateTime = evt.EndDateTime,
                });
            }

            foreach (ClassBookingModel booking in bookingsResponse.Data ?? new List<ClassBookingModel>())
            {
                items.Add(new CalendarItemModel
                {
                    Type = "ClassBooking",
                    Title = $"{booking.ClassTypeName} ({booking.CustomerName})",
                    StartDateTime = booking.StartDateTime,
                    EndDateTime = booking.EndDateTime,
                    Status = booking.Status.ToString(),
                });
            }

            return new JsonResult(items.OrderBy(i => i.StartDateTime).ToList());
        }
    }
}
