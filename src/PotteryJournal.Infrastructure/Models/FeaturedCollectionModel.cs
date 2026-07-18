using System.Collections.Generic;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// The currently homepage-featured collection and its pieces, one representative photo each,
    /// for the rotating display in the section adjacent to the hero.
    /// </summary>
    public class FeaturedCollectionModel
    {
        public string CollectionName { get; set; } = string.Empty;

        public List<FeaturedCollectionPieceModel> Pieces { get; set; } = new List<FeaturedCollectionPieceModel>();
    }
}
