using System.Threading.Tasks;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for the Contact Us form.
    /// </summary>
    public class ContactHandler : IContactHandler
    {
        private readonly IEmailSender _emailSender;
        private readonly IAdminSettingsHandler _adminSettingsHandler;

        /// <summary>
        /// Initializes a new instance of <see cref="ContactHandler"/>.
        /// </summary>
        public ContactHandler(IEmailSender emailSender, IAdminSettingsHandler adminSettingsHandler)
        {
            _emailSender = emailSender;
            _adminSettingsHandler = adminSettingsHandler;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> SubmitAsync(string name, string email, string message)
        {
            HandlerResponse response = new HandlerResponse();

            string normalizedName = name?.Trim() ?? string.Empty;
            string normalizedEmail = email?.Trim() ?? string.Empty;
            string normalizedMessage = message?.Trim() ?? string.Empty;

            if (normalizedName.Length == 0)
            {
                response.AddError("Your name is required.");
            }

            if (normalizedEmail.Length == 0)
            {
                response.AddError("Your email is required.");
            }

            if (normalizedMessage.Length == 0)
            {
                response.AddError("A message is required.");
            }

            if (response.Errors.Count > 0)
            {
                response.IsSuccess = false;
                return response;
            }

            DataHandlerResponse<AdminSettingsModel> settingsResponse = await _adminSettingsHandler.GetAsync();
            string? recipient = settingsResponse.Data?.NotificationRecipientEmail;
            if (string.IsNullOrWhiteSpace(recipient))
            {
                response.AddError("The site isn't configured to receive messages right now. Please try again later.");
                response.IsSuccess = false;
                return response;
            }

            string subject = $"Contact Us message from {normalizedName}";
            string body = $"From: {normalizedName} ({normalizedEmail})\n\n{normalizedMessage}";
            HandlerResponse emailResponse = await _emailSender.SendAsync(recipient, subject, body);
            if (!emailResponse.IsSuccess)
            {
                response.AddError($"Could not send your message: {string.Join(" ", emailResponse.Errors)}");
                response.IsSuccess = false;
                return response;
            }

            response.IsSuccess = true;
            return response;
        }
    }
}
