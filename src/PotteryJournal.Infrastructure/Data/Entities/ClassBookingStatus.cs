namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// The lifecycle state of a <see cref="ClassBooking"/>.
    /// </summary>
    public enum ClassBookingStatus
    {
        Tentative = 0,
        Confirmed = 1,
        Declined = 2,
    }
}
