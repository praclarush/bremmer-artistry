using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// The gallery-tile view of a piece, used by the Pottery Journal grid and the Gallery page.
    /// </summary>
    public class PieceSummaryModel
    {
        public Guid Id { get; set; }

        public int PieceNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Clay { get; set; } = string.Empty;

        public string? Category { get; set; }

        public DateOnly StartedDate { get; set; }

        public string? PrimaryImageFileName { get; set; }
    }
}
