using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages
{
    public class IndexModel : PageModel
    {
        private const int TeaserEventCount = 3;

        private readonly IEventsHandler _eventsHandler;
        private readonly IPieceHandler _pieceHandler;

        public IndexModel(IEventsHandler eventsHandler, IPieceHandler pieceHandler)
        {
            _eventsHandler = eventsHandler;
            _pieceHandler = pieceHandler;
        }

        public List<EventModel> UpcomingEvents { get; private set; } = new List<EventModel>();

        public FeaturedCollectionModel? FeaturedCollection { get; private set; }

        public async Task OnGetAsync()
        {
            DataHandlerResponse<List<EventModel>> eventsResponse = await _eventsHandler.GetUpcomingAsync();
            if (eventsResponse.IsSuccess && eventsResponse.Data is not null)
            {
                UpcomingEvents = eventsResponse.Data.Take(TeaserEventCount).ToList();
            }

            DataHandlerResponse<FeaturedCollectionModel?> featuredResponse = await _pieceHandler.GetFeaturedCollectionAsync();
            if (featuredResponse.IsSuccess)
            {
                FeaturedCollection = featuredResponse.Data;
            }
        }
    }
}
