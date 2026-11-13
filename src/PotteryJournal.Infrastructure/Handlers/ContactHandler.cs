using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        private const int MAX_NAME_LENGTH = 200;
        private const int MAX_EMAIL_LENGTH = 320;
        private const int MAX_MESSAGE_LENGTH = 1000;
        private const string SEND_FAILED_MESSAGE = "We couldn't send your message right now. Please try again in a few minutes.";

        private readonly IEmailSender _emailSender;
        private readonly IAdminSettingsHandler _adminSettingsHandler;
        private readonly ILogger<ContactHandler> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ContactHandler"/>.
        /// </summary>
        public ContactHandler(IEmailSender emailSender, IAdminSettingsHandler adminSettingsHandler, ILogger<ContactHandler> logger)
        {
            _emailSender = emailSender;
            _adminSettingsHandler = adminSettingsHandler;
            _logger = logger;
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
            else if (normalizedName.Length > MAX_NAME_LENGTH)
            {
                response.AddError($"Name must be {MAX_NAME_LENGTH} characters or fewer.");
            }

            if (normalizedEmail.Length == 0)
            {
                response.AddError("Your email is required.");
            }
            else if (normalizedEmail.Length > MAX_EMAIL_LENGTH)
            {
                response.AddError($"Email must be {MAX_EMAIL_LENGTH} characters or fewer.");
            }

            if (normalizedMessage.Length == 0)
            {
                response.AddError("A message is required.");
            }
            else if (normalizedMessage.Length > MAX_MESSAGE_LENGTH)
            {
                response.AddError($"Message must be {MAX_MESSAGE_LENGTH} characters or fewer.");
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
                // The sender's errors carry raw SMTP/exception text -- useful in the log, never
                // something to show a visitor.
                _logger.LogWarning("Contact Us email failed to send: {Errors}", string.Join(" ", emailResponse.Errors));
                response.AddError(SEND_FAILED_MESSAGE);
                response.IsSuccess = false;
                return response;
            }

            response.IsSuccess = true;
            return response;
        }
    }
}
