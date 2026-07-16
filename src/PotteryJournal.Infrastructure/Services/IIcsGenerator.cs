using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Generates downloadable .ics calendar files for events.
    /// </summary>
    public interface IIcsGenerator
    {
        /// <summary>
        /// Generates a single-VEVENT .ics file for the given event.
        /// </summary>
        /// <param name="eventModel">The event to generate a calendar file for.</param>
        /// <returns>A response whose <c>Data</c> is the UTF-8 encoded .ics file content.</returns>
        DataHandlerResponse<byte[]> GenerateEventIcs(EventModel eventModel);
    }
}
