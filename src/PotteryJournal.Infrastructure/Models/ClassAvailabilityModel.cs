using System;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// An admin-defined weekly bookable window for a class type.
    /// </summary>
    public class ClassAvailabilityModel
    {
        public Guid Id { get; set; }

        public Guid ClassTypeId { get; set; }

        public string ClassTypeName { get; set; } = string.Empty;

        public ClassAvailabilityDays DaysOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan LastStartTime { get; set; }

        public int IntervalHours { get; set; }
    }
}
