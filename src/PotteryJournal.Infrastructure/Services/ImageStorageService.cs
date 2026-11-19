using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PotteryJournal.Infrastructure.Constants;
using PotteryJournal.Infrastructure.Options;
using PotteryJournal.SharedKernel.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Resizes and persists uploaded photos, and their thumbnails, to the local uploads volume
    /// using ImageSharp.
    /// </summary>
    public class ImageStorageService : IImageStorageService
    {
        private const int MaxLongEdgePixels = 1600;
        private const int JpegQuality = 82;

        // Grid tiles and cards are at most ~300 CSS px on their short side; 600 covers 2x screens.
        // Sized by the short edge because tiles crop to fill (object-fit: cover).
        private const int ThumbnailShortEdgePixels = 600;
        private const int ThumbnailJpegQuality = 78;
        private const string JpegSearchPattern = "*.jpg";

        private readonly UploadsOptions _uploadsOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="ImageStorageService"/>.
        /// </summary>
        /// <param name="uploadsOptions">The uploads root path configuration.</param>
        public ImageStorageService(IOptions<UploadsOptions> uploadsOptions)
        {
            _uploadsOptions = uploadsOptions.Value;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<string>> SaveResizedJpegAsync(Stream imageStream, string subfolder)
        {
            DataHandlerResponse<string> response = new DataHandlerResponse<string>();

            Image image;
            try
            {
                image = await Image.LoadAsync(imageStream);
            }
            catch (ImageFormatException)
            {
                response.AddError(
                    "That file isn't a supported image format. JPEG, PNG, GIF, WebP, BMP, and TIFF work -- " +
                    "HEIC photos (the iPhone camera default) don't. Switch to Settings > Camera > Formats > " +
                    "\"Most Compatible\" on iPhone, or convert the file to JPEG before uploading.");
                response.IsSuccess = false;
                return response;
            }

            using (image)
            {
                int longEdge = Math.Max(image.Width, image.Height);
                if (longEdge > MaxLongEdgePixels)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(MaxLongEdgePixels, MaxLongEdgePixels),
                    }));
                }

                string directory = Path.Combine(_uploadsOptions.RootPath, subfolder);
                Directory.CreateDirectory(directory);

                string fileName = $"{Guid.NewGuid():N}.jpg";
                string fullPath = Path.Combine(directory, fileName);

                JpegEncoder encoder = new JpegEncoder
                {
                    Quality = JpegQuality,
                };
                await image.SaveAsJpegAsync(fullPath, encoder);

                // The thumbnail is cut from the already-resized photo still in memory, so it costs
                // no second decode.
                await SaveThumbnailAsync(image, ThumbnailPath(subfolder, fileName), CancellationToken.None);

                response.Data = fileName;
                response.IsSuccess = true;
                return response;
            }
        }

        /// <inheritdoc />
        public HandlerResponse Delete(string subfolder, string fileName)
        {
            HandlerResponse response = new HandlerResponse();

            DeleteIfExists(Path.Combine(_uploadsOptions.RootPath, subfolder, fileName));
            DeleteIfExists(ThumbnailPath(subfolder, fileName));

            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<int>> CreateMissingThumbnailsAsync(string subfolder, CancellationToken cancellationToken)
        {
            DataHandlerResponse<int> response = new DataHandlerResponse<int>();

            string directory = Path.Combine(_uploadsOptions.RootPath, subfolder);
            if (!Directory.Exists(directory))
            {
                response.IsSuccess = true;
                return response;
            }

            int createdCount = 0;
            foreach (string photoPath in Directory.EnumerateFiles(directory, JpegSearchPattern, SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(photoPath);
                string thumbnailPath = ThumbnailPath(subfolder, fileName);
                if (File.Exists(thumbnailPath))
                {
                    continue;
                }

                try
                {
                    using (Image image = await Image.LoadAsync(photoPath, cancellationToken))
                    {
                        await SaveThumbnailAsync(image, thumbnailPath, cancellationToken);
                    }

                    createdCount++;
                }
                catch (Exception exception) when (exception is ImageFormatException or IOException)
                {
                    // One unreadable file shouldn't stop the rest; the page falls back to the
                    // full-size photo for it.
                    response.AddWarning($"Couldn't create a thumbnail for {subfolder}/{fileName}: {exception.Message}");
                }
            }

            response.Data = createdCount;
            response.IsSuccess = true;
            return response;
        }

        private static async Task SaveThumbnailAsync(Image source, string thumbnailPath, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

            // ResizeMode.Min never upscales, so a photo already smaller than the thumbnail size is
            // copied at its own size.
            using (Image thumbnail = source.Clone(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Min,
                Size = new Size(ThumbnailShortEdgePixels, ThumbnailShortEdgePixels),
            })))
            {
                JpegEncoder encoder = new JpegEncoder
                {
                    Quality = ThumbnailJpegQuality,
                };

                // Written under a temporary name and moved into place, so a request never sees a
                // half-written thumbnail and an interrupted backfill leaves nothing to skip over.
                string temporaryPath = $"{thumbnailPath}.tmp";
                await thumbnail.SaveAsJpegAsync(temporaryPath, encoder, cancellationToken);
                File.Move(temporaryPath, thumbnailPath, true);
            }
        }

        private string ThumbnailPath(string subfolder, string fileName)
        {
            return Path.Combine(_uploadsOptions.RootPath, subfolder, UploadsSubfolders.Thumbnails, fileName);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
