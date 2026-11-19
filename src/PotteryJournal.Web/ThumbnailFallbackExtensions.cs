using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PotteryJournal.Infrastructure.Constants;

namespace PotteryJournal.Web
{
    /// <summary>
    /// Serves the full-size photo when a requested thumbnail doesn't exist yet, so pages can always
    /// ask for <c>/uploads/{subfolder}/thumbs/{file}</c> without a broken image while
    /// <see cref="ThumbnailBackfillService"/> is still catching up.
    /// </summary>
    public static class ThumbnailFallbackExtensions
    {
        private const string UPLOADS_REQUEST_PATH = "/uploads";
        private const string FALLBACK_ITEM_KEY = "PotteryJournal.ThumbnailFallback";
        private const string FALLBACK_CACHE_CONTROL = "no-cache";

        /// <summary>
        /// Adds middleware that rewrites a request for a missing thumbnail to its full-size photo.
        /// Must run before the static file middleware that serves <c>/uploads</c>.
        /// </summary>
        /// <param name="app">The application pipeline.</param>
        /// <param name="uploadsRootPath">The absolute path of the uploads folder on disk.</param>
        /// <returns>The same pipeline, for chaining.</returns>
        public static IApplicationBuilder UseThumbnailFallback(this IApplicationBuilder app, string uploadsRootPath)
        {
            string thumbnailSegment = $"/{UploadsSubfolders.Thumbnails}/";
            return app.Use(async (context, next) =>
            {
                PathString path = context.Request.Path;
                if (path.StartsWithSegments(UPLOADS_REQUEST_PATH, out PathString relativePath)
                    && relativePath.Value is string relative
                    && relative.Contains(thumbnailSegment)
                    && !File.Exists(Path.Combine(uploadsRootPath, relative.TrimStart('/'))))
                {
                    context.Request.Path = path.Value!.Replace(thumbnailSegment, "/");
                    context.Items[FALLBACK_ITEM_KEY] = true;
                }

                await next(context);
            });
        }

        /// <summary>
        /// Gets the <c>Cache-Control</c> value for an uploads response: the given long-lived value
        /// normally, but <c>no-cache</c> when a missing thumbnail was answered with the full-size
        /// photo, so the browser picks up the real thumbnail once it exists.
        /// </summary>
        /// <param name="context">The current request.</param>
        /// <param name="defaultCacheControl">The value to use for an ordinary response.</param>
        /// <returns>The <c>Cache-Control</c> header value to send.</returns>
        public static string UploadsCacheControl(HttpContext context, string defaultCacheControl)
        {
            return context.Items.ContainsKey(FALLBACK_ITEM_KEY) ? FALLBACK_CACHE_CONTROL : defaultCacheControl;
        }
    }
}
