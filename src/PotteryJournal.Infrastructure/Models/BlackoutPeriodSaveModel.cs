using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// The admin-editable fields of a blackout period, submitted from the create form.
    /// </summary>
    public class BlackoutPeriodSaveModel
    {
        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }

        public string? Reason { get; set; }
    }
}
