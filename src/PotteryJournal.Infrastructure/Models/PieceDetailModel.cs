using System;
using System.Collections.Generic;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// The full worksheet view of a piece, used by the Pottery Journal detail view and the admin edit form.
    /// </summary>
    public class PieceDetailModel
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

        public List<PieceNoteModel> Notes { get; set; } = new List<PieceNoteModel>();

        public List<GlazeApplicationModel> GlazeApplications { get; set; } = new List<GlazeApplicationModel>();

        public List<PieceImageModel> Images { get; set; } = new List<PieceImageModel>();
    }
}
