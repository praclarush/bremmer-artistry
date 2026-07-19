using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Constants;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Events
{
    public class IndexModel : PageModel
    {
        private readonly IEventsHandler _eventsHandler;
        private readonly IImageStorageService _imageStorageService;

        public IndexModel(IEventsHandler eventsHandler, IImageStorageService imageStorageService)
        {
            _eventsHandler = eventsHandler;
            _imageStorageService = imageStorageService;
        }

        public List<EventModel> Events { get; private set; } = new List<EventModel>();

        public async Task OnGetAsync()
        {
            DataHandlerResponse<List<EventModel>> response = await _eventsHandler.GetAllAsync();
            if (response.IsSuccess && response.Data is not null)
            {
                Events = response.Data;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            DataHandlerResponse<EventModel> existing = await _eventsHandler.GetByIdAsync(id);
            if (existing.IsSuccess && existing.Data?.ImageFileName is not null)
            {
                _imageStorageService.Delete(UploadsSubfolders.Events, existing.Data.ImageFileName);
            }

            if (existing.IsSuccess && existing.Data?.FlyerImageFileName is not null)
            {
                _imageStorageService.Delete(UploadsSubfolders.Events, existing.Data.FlyerImageFileName);
            }

            HandlerResponse response = await _eventsHandler.DeleteAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "The event was deleted."
                : string.Join(" ", response.Errors);

            return RedirectToPage();
        }
    }
}
