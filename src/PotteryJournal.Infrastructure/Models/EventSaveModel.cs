using System;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// The admin-editable fields of an event, submitted from the create/edit form.
    /// </summary>
    public class EventSaveModel
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset? EndDateTime { get; set; }

        public string? VenueName { get; set; }

        public string? VenueAddress { get; set; }

        public string? ExternalLinkUrl { get; set; }

        public string? SocialMediaUrl { get; set; }

        public RecurrenceFrequency RecurrenceFrequency { get; set; }

        public int RecurrenceInterval { get; set; } = 1;

        public DateTimeOffset? RecurrenceEndDate { get; set; }
    }
}
