using System;

namespace PotteryJournal.Web.Pages.Admin.Calendar
{
    /// <summary>
    /// A single item on the admin calendar -- either an event occurrence or a class booking,
    /// combined into one shape for client-side rendering.
    /// </summary>
    public class CalendarItemModel
    {
        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset? EndDateTime { get; set; }

        public string? Status { get; set; }
    }
}
