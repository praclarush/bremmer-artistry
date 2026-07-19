using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A computed, currently bookable class slot -- expanded from a <see cref="Data.Entities.ClassAvailability"/>
    /// rule and filtered against blackout periods, the minimum booking lead time, and existing
    /// bookings. Shown on the public Classes page.
    /// </summary>
    public class ClassSlotModel
    {
        public Guid ClassTypeId { get; set; }

        public string ClassTypeName { get; set; } = string.Empty;

        public int MaxCapacity { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }
    }
}
