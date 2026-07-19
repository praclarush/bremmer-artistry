using System;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// The admin-editable fields of a class availability window, submitted from the create form.
    /// </summary>
    public class ClassAvailabilitySaveModel
    {
        public Guid ClassTypeId { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public RecurrenceFrequency RecurrenceFrequency { get; set; }

        public int RecurrenceInterval { get; set; } = 1;

        public DateTimeOffset? RecurrenceEndDate { get; set; }
    }
}
