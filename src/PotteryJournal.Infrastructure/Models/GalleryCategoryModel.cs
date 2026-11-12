namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// One Gallery grouping as shown on a category tile: its name, how many pieces it holds, and
    /// the photo that represents it. Used by the Home page's server-rendered Gallery teaser.
    /// </summary>
    public class GalleryCategoryModel
    {
        public string Name { get; set; } = string.Empty;

        public int PieceCount { get; set; }

        /// <summary>
        /// The first photo of the newest piece in the group that has one, matching the cover the
        /// Gallery page itself picks. Null when no piece in the group has a photo yet.
        /// </summary>
        public string? CoverImageFileName { get; set; }
    }
}
