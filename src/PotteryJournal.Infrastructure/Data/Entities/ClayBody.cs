using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A managed clay body option, assignable to a <see cref="Piece"/>.
    /// </summary>
    public class ClayBody
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
