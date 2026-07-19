using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// The public view of an event, used by the Events page (cards + calendar) and the Home teaser.
    /// </summary>
    public class EventModel
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset? EndDateTime { get; set; }

        public string? VenueName { get; set; }

        public string? VenueAddress { get; set; }

        public string? ImageFileName { get; set; }

        public string? FlyerImageFileName { get; set; }

        public string? ExternalLinkUrl { get; set; }

        public string? SocialMediaUrl { get; set; }
    }
}
