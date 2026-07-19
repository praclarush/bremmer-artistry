using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for events shown on the Events page, calendar, and Home teaser.
    /// </summary>
    public interface IEventsHandler
    {
        /// <summary>
        /// Gets events starting now or later, in chronological order. Recurring events are expanded
        /// into one <see cref="EventModel"/> per upcoming occurrence (within an internal forward
        /// window); non-recurring events are unaffected by that window.
        /// </summary>
        Task<DataHandlerResponse<List<EventModel>>> GetUpcomingAsync();

        /// <summary>
        /// Gets every event, most recently starting first, for the admin CRUD list. Returns one row
        /// per series (the anchor <see cref="Data.Entities.Event"/> row) -- never expanded into
        /// occurrences, since admin edit/delete acts on the whole series.
        /// </summary>
        Task<DataHandlerResponse<List<EventModel>>> GetAllAsync();

        /// <summary>
        /// Gets every event occurrence, past and future, for the public "all events" calendar view.
        /// Recurring events are expanded into one <see cref="EventModel"/> per occurrence (within an
        /// internal past+forward window); non-recurring events are unaffected by that window.
        /// </summary>
        Task<DataHandlerResponse<List<EventModel>>> GetOccurrencesAsync();

        /// <summary>
        /// Gets every event occurrence within the given range, for the admin calendar. Unlike
        /// <see cref="GetOccurrencesAsync"/>, the range is caller-supplied rather than an internal
        /// window, so the admin calendar can page arbitrarily far back or forward.
        /// </summary>
        /// <param name="from">Inclusive start of the range to search.</param>
        /// <param name="to">Inclusive end of the range to search.</param>
        Task<DataHandlerResponse<List<EventModel>>> GetOccurrencesInRangeAsync(DateTimeOffset from, DateTimeOffset to);

        /// <summary>
        /// Gets a single event by its primary key.
        /// </summary>
        /// <param name="id">The event's primary key.</param>
        Task<DataHandlerResponse<EventModel>> GetByIdAsync(Guid id);

        /// <summary>
        /// Creates a new event.
        /// </summary>
        /// <param name="model">The fields submitted from the admin create form.</param>
        /// <param name="createdByEmail">The email of the admin creating the event.</param>
        Task<DataHandlerResponse<Guid>> CreateAsync(EventSaveModel model, string createdByEmail);

        /// <summary>
        /// Updates an existing event's fields.
        /// </summary>
        /// <param name="id">The event's primary key.</param>
        /// <param name="model">The fields submitted from the admin edit form.</param>
        Task<HandlerResponse> UpdateAsync(Guid id, EventSaveModel model);

        /// <summary>
        /// Deletes an event. Does not delete its banner or flyer image files -- callers must do
        /// that via <see cref="Services.IImageStorageService"/>.
        /// </summary>
        /// <param name="id">The event's primary key.</param>
        Task<HandlerResponse> DeleteAsync(Guid id);

        /// <summary>
        /// Sets or replaces an event's banner image file name.
        /// </summary>
        /// <param name="id">The event's primary key.</param>
        /// <param name="fileName">The newly stored file name, as returned by <see cref="Services.IImageStorageService"/>.</param>
        /// <returns>A response whose <c>Data</c> is the previous file name, if any, for the caller to delete from disk.</returns>
        Task<DataHandlerResponse<string?>> SetImageAsync(Guid id, string fileName);

        /// <summary>
        /// Sets or replaces an event's flyer image file name.
        /// </summary>
        /// <param name="id">The event's primary key.</param>
        /// <param name="fileName">The newly stored file name, as returned by <see cref="Services.IImageStorageService"/>.</param>
        /// <returns>A response whose <c>Data</c> is the previous file name, if any, for the caller to delete from disk.</returns>
        Task<DataHandlerResponse<string?>> SetFlyerImageAsync(Guid id, string fileName);
    }
}
