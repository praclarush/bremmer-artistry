using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A single uploaded photo belonging to a <see cref="Piece"/>.
    /// </summary>
    public class PieceImage
    {
        public Guid Id { get; set; }

        public Guid PieceId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public DateTimeOffset CreatedDate { get; set; }

        public Piece? Piece { get; set; }
    }
}
