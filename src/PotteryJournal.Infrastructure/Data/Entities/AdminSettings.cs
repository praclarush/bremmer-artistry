using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// Studio-wide settings editable by an admin without a redeploy. Always exactly one row.
    /// </summary>
    public class AdminSettings
    {
        public Guid Id { get; set; }

        public string NotificationRecipientEmail { get; set; } = string.Empty;

        public int MinimumBookingLeadDays { get; set; } = 2;
    }
}
