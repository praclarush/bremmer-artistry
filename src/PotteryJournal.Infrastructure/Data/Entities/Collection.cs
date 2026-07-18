using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A named grouping of pieces (e.g. "Lightning-Cracked Collection"), independent of
    /// <see cref="Category"/>. At most one collection is featured on the homepage at a time.
    /// </summary>
    public class Collection
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsFeaturedOnHomepage { get; set; }
    }
}
