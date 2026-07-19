using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PotteryJournal.Infrastructure.Options;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Services
{
    /// <summary>
    /// Sends email via SMTP using MailKit, configured from the "Smtp" section.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="SmtpEmailSender"/>.
        /// </summary>
        /// <param name="options">The bound SMTP configuration.</param>
        public SmtpEmailSender(IOptions<SmtpOptions> options)
        {
            _options = options.Value;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> SendAsync(string toAddress, string subject, string body)
        {
            HandlerResponse response = new HandlerResponse();

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            try
            {
                using SmtpClient client = new SmtpClient();
                SecureSocketOptions socketOptions = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(_options.Host, _options.Port, socketOptions);
                if (!string.IsNullOrEmpty(_options.User))
                {
                    await client.AuthenticateAsync(_options.User, _options.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                // Email delivery is an external I/O boundary (DNS, network, auth, remote server
                // errors) -- callers (booking/contact submission) shouldn't crash on a transient
                // SMTP failure, so it's reported as a HandlerResponse error instead of propagating.
                response.AddError($"Could not send email: {ex.Message}");
                response.IsSuccess = false;
            }

            return response;
        }
    }
}
