using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PotteryJournal.Infrastructure.Constants;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web
{
    /// <summary>
    /// Runs once at startup, in the background, to create thumbnails for any stored photos that
    /// lack one: photos uploaded before thumbnails existed, or restored from a backup. New uploads
    /// get their thumbnail at save time, so on a site that's caught up this finds nothing to do.
    /// Until a photo's thumbnail exists, its thumbnail URL serves the full-size photo instead
    /// (see <see cref="ThumbnailFallbackExtensions"/>).
    /// </summary>
    public class ThumbnailBackfillService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ThumbnailBackfillService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ThumbnailBackfillService"/>.
        /// </summary>
        /// <param name="scopeFactory">Creates the scope that resolves the scoped image storage service.</param>
        /// <param name="logger">Logs how many thumbnails were created and any photos that couldn't be read.</param>
        public ThumbnailBackfillService(IServiceScopeFactory scopeFactory, ILogger<ThumbnailBackfillService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IImageStorageService imageStorageService = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
                try
                {
                    await BackfillAsync(imageStorageService, UploadsSubfolders.Pieces, stoppingToken);
                    await BackfillAsync(imageStorageService, UploadsSubfolders.Events, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // App is shutting down; whatever is left gets picked up on the next start.
                }
            }
        }

        private async Task BackfillAsync(IImageStorageService imageStorageService, string subfolder, CancellationToken stoppingToken)
        {
            DataHandlerResponse<int> response = await imageStorageService.CreateMissingThumbnailsAsync(subfolder, stoppingToken);
            foreach (string warning in response.Warnings)
            {
                _logger.LogWarning("{Warning}", warning);
            }

            if (response.Data > 0)
            {
                _logger.LogInformation("Created {Count} missing thumbnails in uploads/{Subfolder}.", response.Data, subfolder);
            }
        }
    }
}
