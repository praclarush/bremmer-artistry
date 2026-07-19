using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Expands a recurrence rule into concrete occurrence start times using Ical.Net's RRULE
    /// engine (also used for .ics export), rather than a hand-rolled recurrence implementation.
    /// </summary>
    public class RecurrenceExpander : IRecurrenceExpander
    {
        /// <inheritdoc />
        public List<DateTimeOffset> Expand(
            DateTimeOffset anchorStart,
            RecurrenceFrequency frequency,
            int interval,
            DateTimeOffset? recurrenceEndDate,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd)
        {
            if (frequency == RecurrenceFrequency.None)
            {
                return anchorStart >= rangeStart && anchorStart <= rangeEnd
                    ? new List<DateTimeOffset> { anchorStart }
                    : new List<DateTimeOffset>();
            }

            CalendarEvent calendarEvent = new CalendarEvent
            {
                Start = new CalDateTime(anchorStart.UtcDateTime, "UTC"),
            };

            RecurrencePattern pattern = new RecurrencePattern(RecurrenceFrequencyMapper.ToFrequencyType(frequency), interval);
            if (recurrenceEndDate.HasValue)
            {
                pattern.Until = recurrenceEndDate.Value.UtcDateTime;
            }

            calendarEvent.RecurrenceRules = new List<RecurrencePattern> { pattern };

            return calendarEvent
                .GetOccurrences(rangeStart.UtcDateTime, rangeEnd.UtcDateTime)
                .Select(occurrence => new DateTimeOffset(occurrence.Period.StartTime.Value, TimeSpan.Zero))
                .OrderBy(occurrenceStart => occurrenceStart)
                .ToList();
        }
    }
}
