namespace PotteryJournal.Infrastructure.Options
{
    /// <summary>
    /// Configuration for where uploaded photos are stored on disk, bound from the "Uploads" section.
    /// </summary>
    public class UploadsOptions
    {
        /// <summary>
        /// Gets or sets the root directory uploaded photos are written under -- expected to be a
        /// mounted Docker volume in production.
        /// </summary>
        public string RootPath { get; set; } = "uploads";
    }
}
