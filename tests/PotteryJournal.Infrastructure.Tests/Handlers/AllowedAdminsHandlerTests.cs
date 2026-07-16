using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
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
            _sut = new AllowedAdminsHandler(_context, new PasswordHasher<AllowedAdmin>());
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
        public async Task ValidateCredentialsAsync_UnknownEmail_ReturnsFailure()
        {
            DataHandlerResponse<AllowedAdminModel> response = await _sut.ValidateCredentialsAsync("nobody@example.com", "whatever");

            Assert.That(response.IsSuccess, Is.False);
        }

        [Test]
        public async Task ValidateCredentialsAsync_CorrectPassword_ReturnsSuccess()
        {
            await _sut.AddAsync("Owner@Example.com", "correct-horse-battery-staple", "Owner", null);

            DataHandlerResponse<AllowedAdminModel> response = await _sut.ValidateCredentialsAsync("owner@example.com", "correct-horse-battery-staple");

            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Data!.Email, Is.EqualTo("owner@example.com"));
        }

        [Test]
        public async Task ValidateCredentialsAsync_WrongPassword_ReturnsFailure()
        {
            await _sut.AddAsync("owner@example.com", "correct-horse-battery-staple", null, null);

            DataHandlerResponse<AllowedAdminModel> response = await _sut.ValidateCredentialsAsync("owner@example.com", "wrong-password");

            Assert.That(response.IsSuccess, Is.False);
        }

        [Test]
        public async Task AddAsync_DuplicateEmail_ReturnsFailure()
        {
            await _sut.AddAsync("owner@example.com", "password1", null, null);

            DataHandlerResponse<Guid> response = await _sut.AddAsync("owner@example.com", "password2", null, null);

            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.Errors, Has.Count.GreaterThan(0));
        }

        [Test]
        public async Task ChangePasswordAsync_UpdatesPassword_NewPasswordValidates()
        {
            DataHandlerResponse<Guid> addResponse = await _sut.AddAsync("owner@example.com", "old-password", null, null);

            await _sut.ChangePasswordAsync(addResponse.Data, "new-password");

            DataHandlerResponse<AllowedAdminModel> oldPasswordResult = await _sut.ValidateCredentialsAsync("owner@example.com", "old-password");
            DataHandlerResponse<AllowedAdminModel> newPasswordResult = await _sut.ValidateCredentialsAsync("owner@example.com", "new-password");

            Assert.That(oldPasswordResult.IsSuccess, Is.False);
            Assert.That(newPasswordResult.IsSuccess, Is.True);
        }

        [Test]
        public async Task EnsureBootstrapAdminAsync_ListEmpty_SeedsAdmin()
        {
            HandlerResponse response = await _sut.EnsureBootstrapAdminAsync("bootstrap@example.com", "bootstrap-password");

            Assert.That(response.IsSuccess, Is.True);
            DataHandlerResponse<AllowedAdminModel> validated = await _sut.ValidateCredentialsAsync("bootstrap@example.com", "bootstrap-password");
            Assert.That(validated.IsSuccess, Is.True);
        }

        [Test]
        public async Task EnsureBootstrapAdminAsync_ListNotEmpty_DoesNotAddBootstrapEmail()
        {
            await _sut.AddAsync("existing@example.com", "password", null, null);

            await _sut.EnsureBootstrapAdminAsync("bootstrap@example.com", "bootstrap-password");

            DataHandlerResponse<AllowedAdminModel> validated = await _sut.ValidateCredentialsAsync("bootstrap@example.com", "bootstrap-password");
            Assert.That(validated.IsSuccess, Is.False);
        }
    }
}
