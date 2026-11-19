namespace PotteryJournal.Infrastructure.Constants
{
    /// <summary>
    /// The subfolders under the uploads root that photos are organized into.
    /// </summary>
    public static class UploadsSubfolders
    {
        public const string Pieces = "pieces";

        public const string Events = "events";

        /// <summary>
        /// The folder inside each photo subfolder that holds its thumbnails, saved under the same
        /// file name as the full-size photo (e.g. <c>pieces/thumbs/abc.jpg</c> for <c>pieces/abc.jpg</c>).
        /// </summary>
        public const string Thumbnails = "thumbs";
    }
}
