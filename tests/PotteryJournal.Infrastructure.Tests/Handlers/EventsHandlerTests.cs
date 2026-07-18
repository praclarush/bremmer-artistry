using System;
using System.Collections.Generic;
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
    public class EventsHandlerTests
    {
        private AppDbContext _context = null!;
        private EventsHandler _sut = null!;

        [SetUp]
        public void SetUp()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _sut = new EventsHandler(_context);
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
        public async Task GetUpcomingAsync_OnlyReturnsFutureEvents()
        {
            await _sut.CreateAsync(BuildSaveModel("Past Show", DateTimeOffset.UtcNow.AddDays(-5)), "admin@example.com");
            await _sut.CreateAsync(BuildSaveModel("Future Show", DateTimeOffset.UtcNow.AddDays(5)), "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Has.Count.EqualTo(1));
            Assert.That(response.Data![0].Title, Is.EqualTo("Future Show"));
        }

        [Test]
        public async Task GetUpcomingAsync_EventStartedEarlierToday_StillReturned()
        {
            await _sut.CreateAsync(BuildSaveModel("Morning Show", DateTimeOffset.UtcNow.AddHours(-3)), "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Has.Count.EqualTo(1));
            Assert.That(response.Data![0].Title, Is.EqualTo("Morning Show"));
        }

        [Test]
        public async Task GetUpcomingAsync_EventEndedEarlierToday_NotReturned()
        {
            EventSaveModel model = BuildSaveModel("Overnight Show", DateTimeOffset.UtcNow.AddDays(-1));
            model.EndDateTime = DateTimeOffset.UtcNow.AddHours(-3);
            await _sut.CreateAsync(model, "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Is.Empty);
        }

        [Test]
        public async Task CreateAsync_SetsCreatedByEmail()
        {
            DataHandlerResponse<Guid> createResponse = await _sut.CreateAsync(BuildSaveModel("Show", DateTimeOffset.UtcNow.AddDays(1)), "owner@example.com");

            DataHandlerResponse<EventModel> detail = await _sut.GetByIdAsync(createResponse.Data);
            Assert.That(detail.IsSuccess, Is.True);
        }

        [Test]
        public async Task SetImageAsync_ReplacesExistingImage_ReturnsPreviousFileName()
        {
            DataHandlerResponse<Guid> createResponse = await _sut.CreateAsync(BuildSaveModel("Show", DateTimeOffset.UtcNow.AddDays(1)), "owner@example.com");
            await _sut.SetImageAsync(createResponse.Data, "first.jpg");

            DataHandlerResponse<string?> secondSet = await _sut.SetImageAsync(createResponse.Data, "second.jpg");

            Assert.That(secondSet.Data, Is.EqualTo("first.jpg"));
        }

        private static EventSaveModel BuildSaveModel(string title, DateTimeOffset startDateTime)
        {
            return new EventSaveModel
            {
                Title = title,
                Description = "A pottery show.",
                StartDateTime = startDateTime,
            };
        }
    }
}
