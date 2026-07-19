using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PotteryJournal.Infrastructure.Options;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Tests.Services
{
    [TestFixture]
    public class SmtpEmailSenderTests
    {
        [Test]
        public async Task SendAsync_CannotConnect_ReturnsFailureInsteadOfThrowing()
        {
            // Loopback with nothing listening -- fails fast with "connection refused", no DNS or
            // external network dependency, so this stays fast and deterministic in any sandbox.
            SmtpOptions options = new SmtpOptions
            {
                Host = "127.0.0.1",
                Port = 1,
                FromAddress = "studio@example.com",
            };
            SmtpEmailSender sut = new SmtpEmailSender(Microsoft.Extensions.Options.Options.Create(options));

            HandlerResponse response = await sut.SendAsync("customer@example.com", "Subject", "Body");

            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.Errors, Has.Count.GreaterThan(0));
        }
    }
}
