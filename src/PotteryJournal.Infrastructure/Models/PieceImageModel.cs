using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A single uploaded photo on a piece, as read through <see cref="Handlers.IPieceHandler"/>.
    /// </summary>
    public class PieceImageModel
    {
        public Guid Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public int SortOrder { get; set; }
    }
}
