using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A single note entry recorded against a <see cref="Piece"/>.
    /// </summary>
    public class PieceNote
    {
        public Guid Id { get; set; }

        public Guid PieceId { get; set; }

        public string? Title { get; set; }

        public string NoteText { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public Piece? Piece { get; set; }
    }
}
