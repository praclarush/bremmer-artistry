namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// How often a recurring <see cref="Event"/> or class availability rule repeats.
    /// </summary>
    public enum RecurrenceFrequency
    {
        None = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
    }
}
