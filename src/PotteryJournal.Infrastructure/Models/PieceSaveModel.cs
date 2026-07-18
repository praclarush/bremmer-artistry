using System;
using System.Collections.Generic;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// The admin-editable fields of a piece, submitted from the create/edit form. On save, the
    /// existing notes and glaze applications for the piece are fully replaced with these lists.
    /// </summary>
    public class PieceSaveModel
    {
        public string Title { get; set; } = string.Empty;

        public Guid? CategoryId { get; set; }

        public bool ShowInGallery { get; set; }

        public Guid? ClayBodyId { get; set; }

        public Guid? CollectionId { get; set; }

        public DateOnly StartedDate { get; set; }

        public DateOnly? FinishedDate { get; set; }

        public string SizeText { get; set; } = string.Empty;

        public string WeightText { get; set; } = string.Empty;

        public string GlazeSummary { get; set; } = string.Empty;

        public string? AttachmentsText { get; set; }

        public List<PieceNoteModel> Notes { get; set; } = new List<PieceNoteModel>();

        public List<GlazeApplicationModel> GlazeApplications { get; set; } = new List<GlazeApplicationModel>();
    }
}
