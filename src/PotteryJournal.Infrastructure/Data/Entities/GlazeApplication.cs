using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A single glaze application recorded against a <see cref="Piece"/>.
    /// </summary>
    public class GlazeApplication
    {
        public Guid Id { get; set; }

        public Guid PieceId { get; set; }

        public string Location { get; set; } = string.Empty;

        public Guid GlazeId { get; set; }

        public Glaze? Glaze { get; set; }

        public int Coats { get; set; }

        public Piece? Piece { get; set; }
    }
}
