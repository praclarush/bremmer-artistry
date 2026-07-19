using System;
using Ical.Net;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Maps <see cref="RecurrenceFrequency"/> to Ical.Net's <see cref="FrequencyType"/>, shared by
    /// <see cref="RecurrenceExpander"/> and <see cref="IcsGenerator"/> so both wrap the same RRULE
    /// engine the same way.
    /// </summary>
    internal static class RecurrenceFrequencyMapper
    {
        /// <summary>
        /// Converts a <see cref="RecurrenceFrequency"/> to the equivalent Ical.Net <see cref="FrequencyType"/>.
        /// </summary>
        /// <param name="frequency">A frequency other than <see cref="RecurrenceFrequency.None"/>.</param>
        public static FrequencyType ToFrequencyType(RecurrenceFrequency frequency)
        {
            switch (frequency)
            {
                case RecurrenceFrequency.Daily:
                    return FrequencyType.Daily;
                case RecurrenceFrequency.Weekly:
                    return FrequencyType.Weekly;
                case RecurrenceFrequency.Monthly:
                    return FrequencyType.Monthly;
                default:
                    throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported recurrence frequency.");
            }
        }
    }
}
