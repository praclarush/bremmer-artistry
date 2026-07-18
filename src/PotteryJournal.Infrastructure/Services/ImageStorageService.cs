using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PotteryJournal.Infrastructure.Options;
using PotteryJournal.SharedKernel.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Resizes and persists uploaded photos to the local uploads volume using ImageSharp.
    /// </summary>
    public class ImageStorageService : IImageStorageService
    {
        private const int MaxLongEdgePixels = 1600;
        private const int JpegQuality = 82;

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

                response.Data = fileName;
                response.IsSuccess = true;
                return response;
            }
        }

        /// <inheritdoc />
        public HandlerResponse Delete(string subfolder, string fileName)
        {
            HandlerResponse response = new HandlerResponse();

            string fullPath = Path.Combine(_uploadsOptions.RootPath, subfolder, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            response.IsSuccess = true;
            return response;
        }
    }
}
