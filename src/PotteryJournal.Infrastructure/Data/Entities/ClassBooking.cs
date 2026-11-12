using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A customer's request to book a class slot. Starts <see cref="ClassBookingStatus.Tentative"/>
    /// and requires admin approval before it's <see cref="ClassBookingStatus.Confirmed"/>. Not linked
    /// to the <see cref="ClassAvailability"/> rule that produced the slot -- occurrences are virtual,
    /// so a booking just records the class type and the chosen start/end time independently.
    /// </summary>
    public class ClassBooking
    {
        public Guid Id { get; set; }

        public Guid ClassTypeId { get; set; }

        public ClassType? ClassType { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public int PartySize { get; set; } = 1;

        public string? Message { get; set; }

        public ClassBookingStatus Status { get; set; }

        public DateTimeOffset CreatedDate { get; set; }

        public DateTimeOffset? DecisionDate { get; set; }
    }
}
