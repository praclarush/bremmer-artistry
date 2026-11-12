using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// An admin-managed date/time range during which class bookings can't be made.
    /// </summary>
    public class BlackoutPeriodModel
    {
        public Guid Id { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }

        public string? Reason { get; set; }
    }
}
