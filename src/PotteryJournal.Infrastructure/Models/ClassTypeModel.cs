using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A managed class type option, as read or written through <see cref="Handlers.IReferenceDataHandler"/>.
    /// </summary>
    public class ClassTypeModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int MaxCapacity { get; set; } = 6;
    }
}
