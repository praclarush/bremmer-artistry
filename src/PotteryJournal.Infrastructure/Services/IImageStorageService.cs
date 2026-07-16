using System.IO;
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
        /// </summary>
        /// <param name="imageStream">The uploaded image content.</param>
        /// <param name="subfolder">The uploads subfolder to save into, e.g. "pieces" or "events".</param>
        /// <returns>A response whose <c>Data</c> is the generated file name on success.</returns>
        Task<DataHandlerResponse<string>> SaveResizedJpegAsync(Stream imageStream, string subfolder);

        /// <summary>
        /// Deletes a previously saved file from the given subfolder, if it exists.
        /// </summary>
        /// <param name="subfolder">The uploads subfolder the file was saved into.</param>
        /// <param name="fileName">The file name to delete.</param>
        HandlerResponse Delete(string subfolder, string fileName);
    }
}
