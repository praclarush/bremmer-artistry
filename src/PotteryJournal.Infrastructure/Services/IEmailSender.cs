using System.Threading.Tasks;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Sends plain-text notification email (class booking requests/confirmations, contact form
    /// submissions) via SMTP.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Sends a plain-text email.
        /// </summary>
        /// <param name="toAddress">The recipient's email address.</param>
        /// <param name="subject">The email subject line.</param>
        /// <param name="body">The plain-text email body.</param>
        Task<HandlerResponse> SendAsync(string toAddress, string subject, string body);
    }
}
