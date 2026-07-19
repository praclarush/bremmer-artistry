using System;
using System.Collections.Generic;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A piece as shown on the public Gallery page: a title, its Gallery grouping, and every photo,
    /// so the lightbox can page through all of a group's images independently of the Pottery
    /// Journal.
    /// </summary>
    public class GalleryPieceModel
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The Gallery tile this piece groups under. This is the piece's Category name when set;
        /// pieces with no Category but a Collection fall back to the Collection name, so
        /// collection-only pieces still surface as a Gallery tile instead of being excluded.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        public DateOnly StartedDate { get; set; }

        public List<string> ImageFileNames { get; set; } = new List<string>();
    }
}
