using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A managed class type option (e.g. Wheel Throw, Hand-Building), assignable to class
    /// availability rules and bookings.
    /// </summary>
    public class ClassType
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int MaxCapacity { get; set; } = 6;
    }
}
