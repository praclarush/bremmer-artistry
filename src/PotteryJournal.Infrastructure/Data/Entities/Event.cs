using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A single public event shown on the Events page and calendar.
    /// </summary>
    public class Event
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset? EndDateTime { get; set; }

        public string? VenueName { get; set; }

        public string? VenueAddress { get; set; }

        public string? ImageFileName { get; set; }

        public string? ExternalLinkUrl { get; set; }

        public DateTimeOffset CreatedDate { get; set; }

        public string CreatedByEmail { get; set; } = string.Empty;
    }
}
