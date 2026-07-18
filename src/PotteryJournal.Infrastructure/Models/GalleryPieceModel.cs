using System;
using System.Collections.Generic;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A piece as shown on the public Gallery page: a title, its category, and every photo, so the
    /// lightbox can page through all of a category's images independently of the Pottery Journal.
    /// </summary>
    public class GalleryPieceModel
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public List<string> ImageFileNames { get; set; } = new List<string>();
    }
}
