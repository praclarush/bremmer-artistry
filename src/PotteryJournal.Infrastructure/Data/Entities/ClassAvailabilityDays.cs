using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// Bitmask of the weekdays a <see cref="ClassAvailability"/> rule is offered on.
    /// </summary>
    [Flags]
    public enum ClassAvailabilityDays
    {
        None = 0,
        Sunday = 1,
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Saturday = 64,
    }
}
