using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Tests.Handlers
{
    [TestFixture]
    public class AllowedAdminsHandlerTests
    {
        private AppDbContext _context = null!;
        private AllowedAdminsHandler _sut = null!;

        [SetUp]
        public void SetUp()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _sut = new AllowedAdminsHandler(_context);
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
        public async Task IsAllowedAsync_EmailNotOnList_ReturnsFalse()
        {
            DataHandlerResponse<bool> response = await _sut.IsAllowedAsync("nobody@example.com");

            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Data, Is.False);
        }

        [Test]
        public async Task IsAllowedAsync_EmailOnListActive_ReturnsTrue()
        {
            await _sut.AddAsync("Owner@Example.com", "Owner", null);

            DataHandlerResponse<bool> response = await _sut.IsAllowedAsync("owner@example.com");

            Assert.That(response.Data, Is.True);
        }

        [Test]
        public async Task AddAsync_DuplicateEmail_ReturnsFailure()
        {
            await _sut.AddAsync("owner@example.com", null, null);

            DataHandlerResponse<Guid> response = await _sut.AddAsync("owner@example.com", null, null);

            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.Errors, Has.Count.GreaterThan(0));
        }

        [Test]
        public async Task EnsureBootstrapAdminAsync_ListEmpty_SeedsAdmin()
        {
            HandlerResponse response = await _sut.EnsureBootstrapAdminAsync("bootstrap@example.com");

            Assert.That(response.IsSuccess, Is.True);
            DataHandlerResponse<bool> isAllowed = await _sut.IsAllowedAsync("bootstrap@example.com");
            Assert.That(isAllowed.Data, Is.True);
        }

        [Test]
        public async Task EnsureBootstrapAdminAsync_ListNotEmpty_DoesNotAddBootstrapEmail()
        {
            await _sut.AddAsync("existing@example.com", null, null);

            await _sut.EnsureBootstrapAdminAsync("bootstrap@example.com");

            DataHandlerResponse<bool> isAllowed = await _sut.IsAllowedAsync("bootstrap@example.com");
            Assert.That(isAllowed.Data, Is.False);
        }
    }
}
