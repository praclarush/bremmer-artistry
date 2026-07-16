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

        public IndexModel(IEventsHandler eventsHandler)
        {
            _eventsHandler = eventsHandler;
        }

        public List<EventModel> UpcomingEvents { get; private set; } = new List<EventModel>();

        public async Task OnGetAsync()
        {
            DataHandlerResponse<List<EventModel>> response = await _eventsHandler.GetUpcomingAsync();
            if (response.IsSuccess && response.Data is not null)
            {
                UpcomingEvents = response.Data.Take(TeaserEventCount).ToList();
            }
        }
    }
}
