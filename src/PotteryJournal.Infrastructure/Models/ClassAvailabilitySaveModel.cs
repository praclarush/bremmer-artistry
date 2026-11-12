using System;
using System.Collections.Generic;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// The admin-editable fields of a class availability window, submitted from the create form.
    /// </summary>
    public class ClassAvailabilitySaveModel
    {
        public Guid ClassTypeId { get; set; }

        public List<ClassAvailabilityDays> DaysOfWeek { get; set; } = new List<ClassAvailabilityDays>();

        public TimeSpan StartTime { get; set; }

        public bool RepeatsMultipleTimesPerDay { get; set; }

        public TimeSpan LastStartTime { get; set; }

        public int IntervalHours { get; set; } = 2;
    }
}
