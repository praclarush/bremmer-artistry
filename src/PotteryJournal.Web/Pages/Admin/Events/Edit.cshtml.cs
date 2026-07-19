using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Constants;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Events
{
    public class EditModel : PageModel
    {
        private readonly IEventsHandler _eventsHandler;
        private readonly IImageStorageService _imageStorageService;

        public EditModel(IEventsHandler eventsHandler, IImageStorageService imageStorageService)
        {
            _eventsHandler = eventsHandler;
            _imageStorageService = imageStorageService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid? Id { get; set; }

        [BindProperty]
        public EventSaveModel Event { get; set; } = new EventSaveModel();

        public string? ImageFileName { get; private set; }

        public string? FlyerImageFileName { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!Id.HasValue)
            {
                Event.StartDateTime = DateTimeOffset.Now;
                return Page();
            }

            DataHandlerResponse<EventModel> response = await _eventsHandler.GetByIdAsync(Id.Value);
            if (!response.IsSuccess || response.Data is null)
            {
                return RedirectToPage("Index");
            }

            EventModel existing = response.Data;
            Event = new EventSaveModel
            {
                Title = existing.Title,
                Description = existing.Description,
                StartDateTime = existing.StartDateTime,
                EndDateTime = existing.EndDateTime,
                VenueName = existing.VenueName,
                VenueAddress = existing.VenueAddress,
                ExternalLinkUrl = existing.ExternalLinkUrl,
                SocialMediaUrl = existing.SocialMediaUrl,
                RecurrenceFrequency = existing.RecurrenceFrequency,
                RecurrenceInterval = existing.RecurrenceInterval,
                RecurrenceEndDate = existing.RecurrenceEndDate,
            };
            ImageFileName = existing.ImageFileName;
            FlyerImageFileName = existing.FlyerImageFileName;

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync(IFormFile? image, IFormFile? flyer)
        {
            if (!ModelState.IsValid)
            {
                if (Id.HasValue)
                {
                    DataHandlerResponse<EventModel> existingResponse = await _eventsHandler.GetByIdAsync(Id.Value);
                    ImageFileName = existingResponse.Data?.ImageFileName;
                    FlyerImageFileName = existingResponse.Data?.FlyerImageFileName;
                }

                return Page();
            }

            Guid eventId;
            if (Id.HasValue)
            {
                HandlerResponse updateResult = await _eventsHandler.UpdateAsync(Id.Value, Event);
                if (!updateResult.IsSuccess)
                {
                    TempData["StatusMessage"] = string.Join(" ", updateResult.Errors);
                    return RedirectToPage("Index");
                }

                eventId = Id.Value;
            }
            else
            {
                string createdByEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
                DataHandlerResponse<Guid> createResult = await _eventsHandler.CreateAsync(Event, createdByEmail);
                if (!createResult.IsSuccess)
                {
                    foreach (string error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }

                    return Page();
                }

                eventId = createResult.Data;
            }

            if (image is not null && image.Length > 0)
            {
                await using System.IO.Stream stream = image.OpenReadStream();
                DataHandlerResponse<string> saveResult = await _imageStorageService.SaveResizedJpegAsync(stream, UploadsSubfolders.Events);
                if (saveResult.IsSuccess && saveResult.Data is not null)
                {
                    DataHandlerResponse<string?> setResult = await _eventsHandler.SetImageAsync(eventId, saveResult.Data);
                    if (setResult.IsSuccess && setResult.Data is not null)
                    {
                        _imageStorageService.Delete(UploadsSubfolders.Events, setResult.Data);
                    }
                }
                else
                {
                    TempData["StatusMessage"] = $"Event saved, but the banner photo couldn't be processed: {string.Join(" ", saveResult.Errors)}";
                    return RedirectToPage("Edit", new { id = eventId });
                }
            }

            if (flyer is not null && flyer.Length > 0)
            {
                await using System.IO.Stream stream = flyer.OpenReadStream();
                DataHandlerResponse<string> saveResult = await _imageStorageService.SaveResizedJpegAsync(stream, UploadsSubfolders.Events);
                if (saveResult.IsSuccess && saveResult.Data is not null)
                {
                    DataHandlerResponse<string?> setResult = await _eventsHandler.SetFlyerImageAsync(eventId, saveResult.Data);
                    if (setResult.IsSuccess && setResult.Data is not null)
                    {
                        _imageStorageService.Delete(UploadsSubfolders.Events, setResult.Data);
                    }
                }
                else
                {
                    TempData["StatusMessage"] = $"Event saved, but the flyer photo couldn't be processed: {string.Join(" ", saveResult.Errors)}";
                    return RedirectToPage("Edit", new { id = eventId });
                }
            }

            TempData["StatusMessage"] = "Event saved.";
            return RedirectToPage("Index");
        }
    }
}
