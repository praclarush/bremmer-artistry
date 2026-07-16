using System.Text;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Generates downloadable .ics calendar files for events using Ical.Net.
    /// </summary>
    public class IcsGenerator : IIcsGenerator
    {
        /// <inheritdoc />
        public DataHandlerResponse<byte[]> GenerateEventIcs(EventModel eventModel)
        {
            DataHandlerResponse<byte[]> response = new DataHandlerResponse<byte[]>();

            Calendar calendar = new Calendar();
            CalendarEvent calendarEvent = new CalendarEvent
            {
                Uid = eventModel.Id.ToString(),
                Summary = eventModel.Title,
                Description = eventModel.Description,
                Location = eventModel.VenueName,
                Start = new CalDateTime(eventModel.StartDateTime.UtcDateTime, "UTC"),
            };

            if (eventModel.EndDateTime.HasValue)
            {
                calendarEvent.End = new CalDateTime(eventModel.EndDateTime.Value.UtcDateTime, "UTC");
            }

            calendar.Events.Add(calendarEvent);

            CalendarSerializer serializer = new CalendarSerializer();
            string icsText = serializer.SerializeToString(calendar);

            response.Data = Encoding.UTF8.GetBytes(icsText);
            response.IsSuccess = true;
            return response;
        }
    }
}
