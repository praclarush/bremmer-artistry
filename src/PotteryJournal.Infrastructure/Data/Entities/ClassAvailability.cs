using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// An admin-defined recurring or one-off bookable window for a class type. Occurrences are
    /// never persisted -- <see cref="Services.IRecurrenceExpander"/> expands this rule into concrete
    /// dates/times on read, the same way a recurring <see cref="Event"/> does.
    /// </summary>
    public class ClassAvailability
    {
        public Guid Id { get; set; }

        public Guid ClassTypeId { get; set; }

        public ClassType? ClassType { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public RecurrenceFrequency RecurrenceFrequency { get; set; }

        public int RecurrenceInterval { get; set; } = 1;

        public DateTimeOffset? RecurrenceEndDate { get; set; }

        public DateTimeOffset CreatedDate { get; set; }
    }
}
