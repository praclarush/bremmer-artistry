using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Resizes and persists uploaded photos to the uploads volume.
    /// </summary>
    public interface IImageStorageService
    {
        /// <summary>
        /// Resizes the given image to a 1600px long edge (never upscaling), re-encodes it as JPEG
        /// quality 82, and saves it under the given subfolder with a newly generated file name.
        /// Also saves a thumbnail (600px short edge) under the same name in the subfolder's
        /// <c>thumbs</c> folder.
        /// </summary>
        /// <param name="imageStream">The uploaded image content.</param>
        /// <param name="subfolder">The uploads subfolder to save into, e.g. "pieces" or "events".</param>
        /// <returns>A response whose <c>Data</c> is the generated file name on success.</returns>
        Task<DataHandlerResponse<string>> SaveResizedJpegAsync(Stream imageStream, string subfolder);

        /// <summary>
        /// Deletes a previously saved file, and its thumbnail, from the given subfolder if they exist.
        /// </summary>
        /// <param name="subfolder">The uploads subfolder the file was saved into.</param>
        /// <param name="fileName">The file name to delete.</param>
        HandlerResponse Delete(string subfolder, string fileName);

        /// <summary>
        /// Creates a thumbnail for every photo in the given subfolder that doesn't have one yet --
        /// the backfill for photos uploaded before thumbnails existed, or copied in by hand. Photos
        /// that already have a thumbnail are skipped, so repeat runs are cheap. A photo that can't
        /// be read adds a warning and is skipped rather than failing the run.
        /// </summary>
        /// <param name="subfolder">The uploads subfolder to scan, e.g. "pieces" or "events".</param>
        /// <param name="cancellationToken">Stops the scan between photos, e.g. on app shutdown.</param>
        /// <returns>A response whose <c>Data</c> is the number of thumbnails created.</returns>
        Task<DataHandlerResponse<int>> CreateMissingThumbnailsAsync(string subfolder, CancellationToken cancellationToken);
    }
}
