namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// Studio-wide settings, always exactly one row -- used for both display and the admin edit form.
    /// </summary>
    public class AdminSettingsModel
    {
        public string NotificationRecipientEmail { get; set; } = string.Empty;

        public int MinimumBookingLeadDays { get; set; } = 2;
    }
}
