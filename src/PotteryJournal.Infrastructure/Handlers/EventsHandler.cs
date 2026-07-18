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
            DateTimeOffset startOfToday = new DateTimeOffset(now.Date, TimeSpan.Zero);
            List<Event> events = await _context.Events
                .AsNoTracking()
                .Where(e => e.EndDateTime.HasValue ? e.EndDateTime.Value >= now : e.StartDateTime >= startOfToday)
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
            Event? eventEntity = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity is null)
            {
                return DataHandlerResponse<EventModel>.NotFound("event", id);
            }

            return new DataHandlerResponse<EventModel>
            {
                Data = ToModel(eventEntity),
                IsSuccess = true,
            };
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
            Event? eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity is null)
            {
                return HandlerResponse.NotFound("event", id);
            }

            ApplySaveModel(eventEntity, model);
            await _context.SaveChangesAsync();

            return new HandlerResponse { IsSuccess = true };
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> DeleteAsync(Guid id)
        {
            Event? eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity is null)
            {
                return HandlerResponse.NotFound("event", id);
            }

            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();

            return new HandlerResponse { IsSuccess = true };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<string?>> SetImageAsync(Guid id, string fileName)
        {
            Event? eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity is null)
            {
                return DataHandlerResponse<string?>.NotFound("event", id);
            }

            string? previousFileName = eventEntity.ImageFileName;
            eventEntity.ImageFileName = fileName;
            await _context.SaveChangesAsync();

            return new DataHandlerResponse<string?>
            {
                Data = previousFileName,
                IsSuccess = true,
            };
        }

        private static void ApplySaveModel(Event eventEntity, EventSaveModel model)
        {
            eventEntity.Title = model.Title;
            eventEntity.Description = model.Description;
            // Npgsql only accepts Offset=0 for "timestamp with time zone" columns. The submitted
            // DateTimeOffset carries whatever offset the app process's local system clock has when
            // the datetime-local input (no offset of its own) gets parsed -- UTC in Docker, but
            // non-zero when running locally (e.g. from Visual Studio). ToUniversalTime() normalizes
            // to Offset=0 without changing the point in time.
            eventEntity.StartDateTime = model.StartDateTime.ToUniversalTime();
            eventEntity.EndDateTime = model.EndDateTime?.ToUniversalTime();
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
