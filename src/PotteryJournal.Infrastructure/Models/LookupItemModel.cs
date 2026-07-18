using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A single managed reference-data option (clay body, glaze, or category), as read or written
    /// through <see cref="Handlers.IReferenceDataHandler"/>.
    /// </summary>
    public class LookupItemModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
