using System;
using System.Collections.Generic;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Expands a recurrence rule into concrete occurrence start times within a date range. No
    /// occurrence is ever persisted -- every caller expands on read, bounded by the range it
    /// queries.
    /// </summary>
    public interface IRecurrenceExpander
    {
        /// <summary>
        /// Computes every occurrence start time of the given rule that falls within
        /// <paramref name="rangeStart"/> and <paramref name="rangeEnd"/> (inclusive).
        /// </summary>
        /// <param name="anchorStart">The first/anchor occurrence's start date and time.</param>
        /// <param name="frequency">The recurrence frequency. <see cref="RecurrenceFrequency.None"/> yields at most the anchor occurrence itself.</param>
        /// <param name="interval">The recurrence interval, e.g. 2 for "every 2 weeks". Ignored when frequency is <see cref="RecurrenceFrequency.None"/>.</param>
        /// <param name="recurrenceEndDate">The last date recurrence may occur on, or null to recur indefinitely (bounded only by the query range).</param>
        /// <param name="rangeStart">The inclusive start of the date range to search.</param>
        /// <param name="rangeEnd">The inclusive end of the date range to search.</param>
        List<DateTimeOffset> Expand(
            DateTimeOffset anchorStart,
            RecurrenceFrequency frequency,
            int interval,
            DateTimeOffset? recurrenceEndDate,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd);
    }
}
