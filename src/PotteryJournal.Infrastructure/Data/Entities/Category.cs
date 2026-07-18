using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A managed category option, assignable to a <see cref="Piece"/> to group it on the Gallery page.
    /// </summary>
    public class Category
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
