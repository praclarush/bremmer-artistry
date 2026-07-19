using System;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// An admin-defined recurring or one-off bookable window for a class type.
    /// </summary>
    public class ClassAvailabilityModel
    {
        public Guid Id { get; set; }

        public Guid ClassTypeId { get; set; }

        public string ClassTypeName { get; set; } = string.Empty;

        public DateTimeOffset StartDateTime { get; set; }

        public RecurrenceFrequency RecurrenceFrequency { get; set; }

        public int RecurrenceInterval { get; set; } = 1;

        public DateTimeOffset? RecurrenceEndDate { get; set; }
    }
}
