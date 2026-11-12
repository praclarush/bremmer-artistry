using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for the studio's admin-editable settings.
    /// </summary>
    public class AdminSettingsHandler : IAdminSettingsHandler
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="AdminSettingsHandler"/>.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public AdminSettingsHandler(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<AdminSettingsModel>> GetAsync()
        {
            AdminSettings settings = await GetOrCreateSettingsAsync();

            return new DataHandlerResponse<AdminSettingsModel>
            {
                Data = ToModel(settings),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> UpdateAsync(AdminSettingsModel model)
        {
            HandlerResponse response = new HandlerResponse();

            string normalizedEmail = model.NotificationRecipientEmail?.Trim() ?? string.Empty;
            if (normalizedEmail.Length == 0)
            {
                response.AddError("A notification recipient email is required.");
            }

            if (model.MinimumBookingLeadDays < 0)
            {
                response.AddError("Minimum booking lead days can't be negative.");
            }

            if (response.Errors.Count > 0)
            {
                response.IsSuccess = false;
                return response;
            }

            AdminSettings settings = await GetOrCreateSettingsAsync();
            settings.NotificationRecipientEmail = normalizedEmail;
            settings.MinimumBookingLeadDays = model.MinimumBookingLeadDays;
            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        private async Task<AdminSettings> GetOrCreateSettingsAsync()
        {
            AdminSettings? settings = await _context.AdminSettings.FirstOrDefaultAsync();
            if (settings is null)
            {
                settings = new AdminSettings();
                _context.AdminSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        private static AdminSettingsModel ToModel(AdminSettings settings)
        {
            return new AdminSettingsModel
            {
                NotificationRecipientEmail = settings.NotificationRecipientEmail,
                MinimumBookingLeadDays = settings.MinimumBookingLeadDays,
            };
        }
    }
}
