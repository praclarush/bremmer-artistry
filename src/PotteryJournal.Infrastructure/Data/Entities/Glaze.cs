using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A managed glaze option, assignable to a <see cref="GlazeApplication"/>.
    /// </summary>
    public class Glaze
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
