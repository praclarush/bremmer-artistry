using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A managed collection, as read or written through <see cref="Handlers.IReferenceDataHandler"/>.
    /// </summary>
    public class CollectionModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsFeaturedOnHomepage { get; set; }

        public int PieceCount { get; set; }
    }
}
