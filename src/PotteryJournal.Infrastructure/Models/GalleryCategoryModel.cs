namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// One Gallery page tile: a category tag and a representative image drawn from the most
    /// recent piece using that category.
    /// </summary>
    public class GalleryCategoryModel
    {
        public string Category { get; set; } = string.Empty;

        public string? RepresentativeImageFileName { get; set; }

        public int PieceCount { get; set; }
    }
}
