using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Tests.Handlers
{
    [TestFixture]
    public class ContactHandlerTests
    {
        private AppDbContext _context = null!;
        private AdminSettingsHandler _adminSettingsHandler = null!;
        private Mock<IEmailSender> _emailSenderMock = null!;
        private ContactHandler _sut = null!;

        [SetUp]
        public void SetUp()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _adminSettingsHandler = new AdminSettingsHandler(_context);
            _emailSenderMock = new Mock<IEmailSender>();
            _emailSenderMock
                .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HandlerResponse { IsSuccess = true });

            _sut = new ContactHandler(_emailSenderMock.Object, _adminSettingsHandler);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _context.Database.EnsureDeleted();
                _context.Dispose();
            }
            catch
            {
            }
        }

        [Test]
        public async Task SubmitAsync_ValidMessage_SendsToConfiguredRecipient()
        {
            await _adminSettingsHandler.UpdateAsync(new AdminSettingsModel
            {
                NotificationRecipientEmail = "studio@example.com",
                MinimumBookingLeadDays = 2,
            });

            HandlerResponse response = await _sut.SubmitAsync("Jane Doe", "customer@example.com", "Hello there.");

            Assert.That(response.IsSuccess, Is.True);
            _emailSenderMock.Verify(e => e.SendAsync("studio@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task SubmitAsync_EmptyMessage_ReturnsFailureWithoutSendingEmail()
        {
            await _adminSettingsHandler.UpdateAsync(new AdminSettingsModel
            {
                NotificationRecipientEmail = "studio@example.com",
                MinimumBookingLeadDays = 2,
            });

            HandlerResponse response = await _sut.SubmitAsync("Jane Doe", "customer@example.com", "  ");

            Assert.That(response.IsSuccess, Is.False);
            _emailSenderMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SubmitAsync_NoRecipientConfigured_ReturnsFailure()
        {
            HandlerResponse response = await _sut.SubmitAsync("Jane Doe", "customer@example.com", "Hello there.");

            Assert.That(response.IsSuccess, Is.False);
        }

        [Test]
        public async Task SubmitAsync_EmailSendFails_ReturnsFailure()
        {
            await _adminSettingsHandler.UpdateAsync(new AdminSettingsModel
            {
                NotificationRecipientEmail = "studio@example.com",
                MinimumBookingLeadDays = 2,
            });
            _emailSenderMock
                .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HandlerResponse { IsSuccess = false });

            HandlerResponse response = await _sut.SubmitAsync("Jane Doe", "customer@example.com", "Hello there.");

            Assert.That(response.IsSuccess, Is.False);
        }
    }
}
