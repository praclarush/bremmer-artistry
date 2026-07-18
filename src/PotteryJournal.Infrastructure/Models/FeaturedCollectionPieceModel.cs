namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// One piece's representative photo within the homepage's featured-collection rotation.
    /// </summary>
    public class FeaturedCollectionPieceModel
    {
        public string Title { get; set; } = string.Empty;

        public string ImageFileName { get; set; } = string.Empty;
    }
}
