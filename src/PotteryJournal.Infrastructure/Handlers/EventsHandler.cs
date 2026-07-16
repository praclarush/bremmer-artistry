using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for events shown on the Events page, calendar, and Home teaser.
    /// </summary>
    public class EventsHandler : IEventsHandler
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="EventsHandler"/>.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public EventsHandler(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<EventModel>>> GetUpcomingAsync()
        {
            DataHandlerResponse<List<EventModel>> response = new DataHandlerResponse<List<EventModel>>();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            List<Event> events = await _context.Events
                .AsNoTracking()
                .Where(e => e.StartDateTime >= now)
                .OrderBy(e => e.StartDateTime)
                .ToListAsync();

            response.Data = events.Select(ToModel).ToList();
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<EventModel>>> GetAllAsync()
        {
            DataHandlerResponse<List<EventModel>> response = new DataHandlerResponse<List<EventModel>>();

            List<Event> events = await _context.Events
                .AsNoTracking()
                .OrderByDescending(e => e.StartDateTime)
                .ToListAsync();

            response.Data = events.Select(ToModel).ToList();
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<EventModel>> GetByIdAsync(Guid id)
        {
            DataHandlerResponse<EventModel> response = new DataHandlerResponse<EventModel>();

            Event? eventEntity = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity is null)
            {
                response.AddError($"No event was found with id {id}.");
                response.IsSuccess = false;
                return response;
            }

            response.Data = ToModel(eventEntity);
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> CreateAsync(EventSaveModel model, string createdByEmail)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();

            Event eventEntity = new Event
            {
                CreatedDate = DateTimeOffset.UtcNow,
                CreatedByEmail = createdByEmail,
            };
            ApplySaveModel(eventEntity, model);

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            response.Data = eventEntity.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> UpdateAsync(Guid id, EventSaveModel model)
        {
            HandlerResponse response = new HandlerResponse();

            Event? eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity is null)
            {
                response.AddError($"No event was found with id {id}.");
                response.IsSuccess = false;
                return response;
            }

            ApplySaveModel(eventEntity, model);
            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> DeleteAsync(Guid id)
        {
            HandlerResponse response = new HandlerResponse();

            Event? eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity is null)
            {
                response.AddError($"No event was found with id {id}.");
                response.IsSuccess = false;
                return response;
            }

            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<string?>> SetImageAsync(Guid id, string fileName)
        {
            DataHandlerResponse<string?> response = new DataHandlerResponse<string?>();

            Event? eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity is null)
            {
                response.AddError($"No event was found with id {id}.");
                response.IsSuccess = false;
                return response;
            }

            response.Data = eventEntity.ImageFileName;
            eventEntity.ImageFileName = fileName;
            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        private static void ApplySaveModel(Event eventEntity, EventSaveModel model)
        {
            eventEntity.Title = model.Title;
            eventEntity.Description = model.Description;
            eventEntity.StartDateTime = model.StartDateTime;
            eventEntity.EndDateTime = model.EndDateTime;
            eventEntity.VenueName = model.VenueName;
            eventEntity.VenueAddress = model.VenueAddress;
            eventEntity.ExternalLinkUrl = model.ExternalLinkUrl;
        }

        private static EventModel ToModel(Event eventEntity)
        {
            return new EventModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                StartDateTime = eventEntity.StartDateTime,
                EndDateTime = eventEntity.EndDateTime,
                VenueName = eventEntity.VenueName,
                VenueAddress = eventEntity.VenueAddress,
                ImageFileName = eventEntity.ImageFileName,
                ExternalLinkUrl = eventEntity.ExternalLinkUrl,
            };
        }
    }
}
