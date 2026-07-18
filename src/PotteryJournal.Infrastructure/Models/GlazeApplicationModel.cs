using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A single glaze application on a piece, as read or written through <see cref="Handlers.IPieceHandler"/>.
    /// </summary>
    public class GlazeApplicationModel
    {
        public string Location { get; set; } = string.Empty;

        public Guid? GlazeId { get; set; }

        public string GlazeName { get; set; } = string.Empty;

        public int Coats { get; set; }
    }
}
