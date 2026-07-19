using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// An admin-managed date/time range during which class bookings can't be made.
    /// </summary>
    public class BlackoutPeriod
    {
        public Guid Id { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }

        public string? Reason { get; set; }

        public DateTimeOffset CreatedDate { get; set; }
    }
}
