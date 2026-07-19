using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// An admin-defined weekly bookable window for a class type: the set of weekdays it's offered
    /// on, and one or more start times each of those days. Occurrences are never persisted -- <see
    /// cref="Handlers.ClassesHandler.GetAvailableSlotsAsync"/> walks each day in the requested range
    /// and, whenever that day's weekday is in <see cref="DaysOfWeek"/>, emits an occurrence at <see
    /// cref="StartTime"/>, then every <see cref="IntervalHours"/> after that up to and including
    /// <see cref="LastStartTime"/>. When the class only runs once a day, <see cref="LastStartTime"/>
    /// equals <see cref="StartTime"/>. The rule itself has no end date; it recurs indefinitely until
    /// deleted, with <see cref="BlackoutPeriod"/> as the sole mechanism for excluding specific
    /// dates/times.
    /// </summary>
    public class ClassAvailability
    {
        public Guid Id { get; set; }

        public Guid ClassTypeId { get; set; }

        public ClassType? ClassType { get; set; }

        public ClassAvailabilityDays DaysOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan LastStartTime { get; set; }

        public int IntervalHours { get; set; } = 1;

        public DateTimeOffset CreatedDate { get; set; }
    }
}
