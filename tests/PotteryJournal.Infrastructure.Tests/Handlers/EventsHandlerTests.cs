using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Services;
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
            _sut = new EventsHandler(_context, new RecurrenceExpander());
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

        [Test]
        public async Task GetUpcomingAsync_WeeklyRecurringEvent_ExpandsMultipleOccurrences()
        {
            EventSaveModel model = BuildRecurringSaveModel(
                "Weekly Studio Night",
                DateTimeOffset.UtcNow.AddDays(1),
                RecurrenceFrequency.Weekly,
                interval: 1);
            await _sut.CreateAsync(model, "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Has.Count.GreaterThan(1));
            Assert.That(response.Data!.Select(e => e.Title), Is.All.EqualTo("Weekly Studio Night"));
            Assert.That(response.Data!, Is.Ordered.By(nameof(EventModel.StartDateTime)));
        }

        [Test]
        public async Task GetUpcomingAsync_DailyRecurrenceEveryOtherDay_SpacesOccurrencesTwoDaysApart()
        {
            EventSaveModel model = BuildRecurringSaveModel(
                "Every Other Day Class",
                DateTimeOffset.UtcNow.AddDays(1),
                RecurrenceFrequency.Daily,
                interval: 2);
            await _sut.CreateAsync(model, "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            List<DateTimeOffset> starts = response.Data!.Select(e => e.StartDateTime).OrderBy(d => d).ToList();
            Assert.That(starts, Has.Count.GreaterThan(1));
            for (int i = 1; i < starts.Count; i++)
            {
                Assert.That((starts[i] - starts[i - 1]).TotalDays, Is.EqualTo(2).Within(0.001));
            }
        }

        [Test]
        public async Task GetUpcomingAsync_RecurringEventWithEndDate_StopsAtEndDate()
        {
            DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(1);
            DateTimeOffset endDate = start.AddDays(10);
            EventSaveModel model = BuildRecurringSaveModel("Short Series", start, RecurrenceFrequency.Weekly, interval: 1, recurrenceEndDate: endDate);
            await _sut.CreateAsync(model, "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Has.Count.EqualTo(2));
            Assert.That(response.Data!.All(e => e.StartDateTime <= endDate), Is.True);
        }

        [Test]
        public async Task GetOccurrencesAsync_RecurringEventAnchoredInPast_IncludesPastOccurrences()
        {
            EventSaveModel model = BuildRecurringSaveModel(
                "Long Running Class",
                DateTimeOffset.UtcNow.AddDays(-30),
                RecurrenceFrequency.Weekly,
                interval: 1);
            await _sut.CreateAsync(model, "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetOccurrencesAsync();

            Assert.That(response.Data!.Any(e => e.StartDateTime < DateTimeOffset.UtcNow), Is.True);
        }

        [Test]
        public async Task UpdateAsync_TogglesRecurrenceBackToNone_ClearsIntervalAndEndDate()
        {
            DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(1);
            EventSaveModel recurringModel = BuildRecurringSaveModel("Toggled Series", start, RecurrenceFrequency.Weekly, interval: 3, recurrenceEndDate: start.AddMonths(2));
            DataHandlerResponse<Guid> createResponse = await _sut.CreateAsync(recurringModel, "admin@example.com");

            EventSaveModel updatedModel = BuildSaveModel("Toggled Series", start);
            updatedModel.RecurrenceFrequency = RecurrenceFrequency.None;
            await _sut.UpdateAsync(createResponse.Data, updatedModel);

            DataHandlerResponse<EventModel> detail = await _sut.GetByIdAsync(createResponse.Data);
            Assert.That(detail.Data!.RecurrenceInterval, Is.EqualTo(1));
            Assert.That(detail.Data!.RecurrenceEndDate, Is.Null);
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

        private static EventSaveModel BuildRecurringSaveModel(
            string title,
            DateTimeOffset startDateTime,
            RecurrenceFrequency frequency,
            int interval,
            DateTimeOffset? recurrenceEndDate = null)
        {
            EventSaveModel model = BuildSaveModel(title, startDateTime);
            model.RecurrenceFrequency = frequency;
            model.RecurrenceInterval = interval;
            model.RecurrenceEndDate = recurrenceEndDate;
            return model;
        }
    }
}
