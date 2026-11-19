namespace PotteryJournal.Infrastructure.Options
{
    /// <summary>
    /// The studio's local time zone, bound from the "Site" configuration section. Decides where
    /// "today" begins for date-based business rules such as which events are still upcoming.
    /// </summary>
    public class StudioTimeOptions
    {
        /// <summary>
        /// The default studio time zone, used when <see cref="TimeZone"/> is not configured.
        /// </summary>
        public const string DEFAULT_TIME_ZONE = "America/Chicago";

        /// <summary>
        /// Gets or sets the IANA time zone id of the studio (for example "America/Chicago").
        /// </summary>
        public string TimeZone { get; set; } = DEFAULT_TIME_ZONE;
    }
}
