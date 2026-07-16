using System;
using System.Collections.Generic;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A single pottery piece entry in the Pottery Journal catalog.
    /// </summary>
    public class Piece
    {
        public Guid Id { get; set; }

        public int PieceNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string Clay { get; set; } = string.Empty;

        public DateOnly StartedDate { get; set; }

        public DateOnly? FinishedDate { get; set; }

        public string SizeText { get; set; } = string.Empty;

        public string WeightText { get; set; } = string.Empty;

        public string GlazeSummary { get; set; } = string.Empty;

        public string? AttachmentsText { get; set; }

        public DateTimeOffset CreatedDate { get; set; }

        public string CreatedByEmail { get; set; } = string.Empty;

        public List<PieceNote> Notes { get; set; } = new List<PieceNote>();

        public List<PieceImage> Images { get; set; } = new List<PieceImage>();

        public List<GlazeApplication> GlazeApplications { get; set; } = new List<GlazeApplication>();
    }
}
