using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Tests.Handlers
{
    [TestFixture]
    public class AdminSettingsHandlerTests
    {
        private AppDbContext _context = null!;
        private AdminSettingsHandler _sut = null!;

        [SetUp]
        public void SetUp()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _sut = new AdminSettingsHandler(_context);
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
        public async Task GetAsync_NoRowYet_CreatesRowWithDefaults()
        {
            DataHandlerResponse<AdminSettingsModel> response = await _sut.GetAsync();

            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Data!.MinimumBookingLeadDays, Is.EqualTo(2));
            Assert.That(await _context.AdminSettings.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task UpdateAsync_ValidModel_PersistsChanges()
        {
            AdminSettingsModel model = new AdminSettingsModel
            {
                NotificationRecipientEmail = "studio@example.com",
                MinimumBookingLeadDays = 5,
            };

            HandlerResponse updateResponse = await _sut.UpdateAsync(model);
            DataHandlerResponse<AdminSettingsModel> getResponse = await _sut.GetAsync();

            Assert.That(updateResponse.IsSuccess, Is.True);
            Assert.That(getResponse.Data!.NotificationRecipientEmail, Is.EqualTo("studio@example.com"));
            Assert.That(getResponse.Data!.MinimumBookingLeadDays, Is.EqualTo(5));
        }

        [Test]
        public async Task UpdateAsync_EmptyEmail_ReturnsFailure()
        {
            AdminSettingsModel model = new AdminSettingsModel
            {
                NotificationRecipientEmail = "  ",
                MinimumBookingLeadDays = 2,
            };

            HandlerResponse response = await _sut.UpdateAsync(model);

            Assert.That(response.IsSuccess, Is.False);
        }

        [Test]
        public async Task UpdateAsync_NegativeLeadDays_ReturnsFailure()
        {
            AdminSettingsModel model = new AdminSettingsModel
            {
                NotificationRecipientEmail = "studio@example.com",
                MinimumBookingLeadDays = -1,
            };

            HandlerResponse response = await _sut.UpdateAsync(model);

            Assert.That(response.IsSuccess, Is.False);
        }
    }
}
