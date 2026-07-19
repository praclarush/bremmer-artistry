using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for events shown on the Events page, calendar, and Home teaser.
    /// </summary>
    public class EventsHandler : IEventsHandler
    {
        // How far into the future recurring events are expanded for "what's upcoming" display.
        // Keeps indefinitely-recurring events (no RecurrenceEndDate) from generating unbounded
        // occurrence lists. Non-recurring events are never bounded by this.
        private static readonly TimeSpan _recurringForwardWindow = TimeSpan.FromDays(180);

        // How far into the past recurring events are expanded for the "all events" calendar view,
        // which (unlike GetUpcomingAsync) also shows history. Generous enough that browsing the
        // calendar a few months back never hits the edge.
        private static readonly TimeSpan _recurringPastWindow = TimeSpan.FromDays(730);

        private readonly AppDbContext _context;
        private readonly IRecurrenceExpander _recurrenceExpander;

        /// <summary>
        /// Initializes a new instance of <see cref="EventsHandler"/>.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="recurrenceExpander">Expands recurring events into concrete occurrences.</param>
        public EventsHandler(AppDbContext context, IRecurrenceExpander recurrenceExpander)
        {
            _context = context;
            _recurrenceExpander = recurrenceExpander;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<EventModel>>> GetUpcomingAsync()
        {
            DataHandlerResponse<List<EventModel>> response = new DataHandlerResponse<List<EventModel>>();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset startOfToday = new DateTimeOffset(now.Date, TimeSpan.Zero);
            DateTimeOffset forwardWindowEnd = now.Add(_recurringForwardWindow);

            static bool IsUpcoming(DateTimeOffset? endDateTime, DateTimeOffset startDateTime, DateTimeOffset upcomingNow, DateTimeOffset upcomingStartOfToday)
            {
                return endDateTime.HasValue ? endDateTime.Value >= upcomingNow : startDateTime >= upcomingStartOfToday;
            }

            List<Event> events = await _context.Events.AsNoTracking().ToListAsync();

            List<EventModel> upcoming = new List<EventModel>();
            foreach (Event eventEntity in events)
            {
                if (eventEntity.RecurrenceFrequency == RecurrenceFrequency.None)
                {
                    if (IsUpcoming(eventEntity.EndDateTime, eventEntity.StartDateTime, now, startOfToday))
                    {
                        upcoming.Add(ToModel(eventEntity));
                    }

                    continue;
                }

                foreach (EventModel occurrence in ExpandRecurringOccurrences(eventEntity, startOfToday, forwardWindowEnd))
                {
                    if (IsUpcoming(occurrence.EndDateTime, occurrence.StartDateTime, now, startOfToday))
                    {
                        upcoming.Add(occurrence);
                    }
                }
            }

            response.Data = upcoming.OrderBy(e => e.StartDateTime).ToList();
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
        public async Task<DataHandlerResponse<List<EventModel>>> GetOccurrencesAsync()
        {
            DataHandlerResponse<List<EventModel>> response = new DataHandlerResponse<List<EventModel>>();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset windowStart = now.Subtract(_recurringPastWindow);
            DateTimeOffset windowEnd = now.Add(_recurringForwardWindow);

            List<Event> events = await _context.Events.AsNoTracking().ToListAsync();

            List<EventModel> occurrences = new List<EventModel>();
            foreach (Event eventEntity in events)
            {
                if (eventEntity.RecurrenceFrequency == RecurrenceFrequency.None)
                {
                    occurrences.Add(ToModel(eventEntity));
                    continue;
                }

                occurrences.AddRange(ExpandRecurringOccurrences(eventEntity, windowStart, windowEnd));
            }

            response.Data = occurrences.OrderByDescending(e => e.StartDateTime).ToList();
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<EventModel>>> GetOccurrencesInRangeAsync(DateTimeOffset from, DateTimeOffset to)
        {
            DataHandlerResponse<List<EventModel>> response = new DataHandlerResponse<List<EventModel>>();

            List<Event> events = await _context.Events.AsNoTracking().ToListAsync();

            List<EventModel> occurrences = new List<EventModel>();
            foreach (Event eventEntity in events)
            {
                if (eventEntity.RecurrenceFrequency == RecurrenceFrequency.None)
                {
                    if (eventEntity.StartDateTime >= from && eventEntity.StartDateTime <= to)
                    {
                        occurrences.Add(ToModel(eventEntity));
                    }

                    continue;
                }

                occurrences.AddRange(ExpandRecurringOccurrences(eventEntity, from, to));
            }

            response.Data = occurrences.OrderBy(e => e.StartDateTime).ToList();
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

        /// <inheritdoc />
        public async Task<DataHandlerResponse<string?>> SetFlyerImageAsync(Guid id, string fileName)
        {
            Event? eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity is null)
            {
                return DataHandlerResponse<string?>.NotFound("event", id);
            }

            string? previousFileName = eventEntity.FlyerImageFileName;
            eventEntity.FlyerImageFileName = fileName;
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
            eventEntity.SocialMediaUrl = model.SocialMediaUrl;
            eventEntity.RecurrenceFrequency = model.RecurrenceFrequency;
            // Normalize away stale interval/end-date values once recurrence is turned back off, so
            // a series that's toggled None -> Weekly -> None never carries forward a forgotten
            // interval or end date if it's turned on again without the admin revisiting those fields.
            eventEntity.RecurrenceInterval = model.RecurrenceFrequency == RecurrenceFrequency.None
                ? 1
                : Math.Max(1, model.RecurrenceInterval);
            eventEntity.RecurrenceEndDate = model.RecurrenceFrequency == RecurrenceFrequency.None
                ? null
                : model.RecurrenceEndDate?.ToUniversalTime();
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
                FlyerImageFileName = eventEntity.FlyerImageFileName,
                ExternalLinkUrl = eventEntity.ExternalLinkUrl,
                SocialMediaUrl = eventEntity.SocialMediaUrl,
                RecurrenceFrequency = eventEntity.RecurrenceFrequency,
                RecurrenceInterval = eventEntity.RecurrenceInterval,
                RecurrenceEndDate = eventEntity.RecurrenceEndDate,
            };
        }

        // Expands a recurring event into one EventModel per occurrence within [windowStart, windowEnd].
        // Only called for events whose RecurrenceFrequency isn't None -- see callers.
        private List<EventModel> ExpandRecurringOccurrences(Event eventEntity, DateTimeOffset windowStart, DateTimeOffset windowEnd)
        {
            TimeSpan? duration = eventEntity.EndDateTime.HasValue
                ? eventEntity.EndDateTime.Value - eventEntity.StartDateTime
                : (TimeSpan?)null;

            List<DateTimeOffset> occurrenceStarts = _recurrenceExpander.Expand(
                eventEntity.StartDateTime,
                eventEntity.RecurrenceFrequency,
                eventEntity.RecurrenceInterval,
                eventEntity.RecurrenceEndDate,
                windowStart,
                windowEnd);

            List<EventModel> occurrences = new List<EventModel>();
            foreach (DateTimeOffset occurrenceStart in occurrenceStarts)
            {
                EventModel model = ToModel(eventEntity);
                model.StartDateTime = occurrenceStart;
                model.EndDateTime = duration.HasValue ? occurrenceStart + duration.Value : (DateTimeOffset?)null;
                occurrences.Add(model);
            }

            return occurrences;
        }
    }
}
