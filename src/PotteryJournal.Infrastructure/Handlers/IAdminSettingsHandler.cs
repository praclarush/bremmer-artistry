using System.Threading.Tasks;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for the studio's admin-editable settings (notification recipient, minimum
    /// class booking lead time).
    /// </summary>
    public interface IAdminSettingsHandler
    {
        /// <summary>
        /// Gets the studio's settings, creating the single row with defaults on first read if the
        /// table is empty.
        /// </summary>
        Task<DataHandlerResponse<AdminSettingsModel>> GetAsync();

        /// <summary>
        /// Updates the studio's settings.
        /// </summary>
        /// <param name="model">The submitted fields from the admin settings form.</param>
        Task<HandlerResponse> UpdateAsync(AdminSettingsModel model);
    }
}
