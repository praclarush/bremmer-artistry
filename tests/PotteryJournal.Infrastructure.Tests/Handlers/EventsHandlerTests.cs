using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Options;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Tests.Handlers
{
    [TestFixture]
    public class EventsHandlerTests
    {
        private const string STUDIO_TIME_ZONE = "America/Chicago";

        // Noon Central (CST, UTC-6) on 2026-11-12. A fixed clock keeps day-boundary logic
        // independent of when the suite runs.
        private static readonly DateTimeOffset _defaultNow = new DateTimeOffset(2026, 11, 12, 18, 0, 0, TimeSpan.Zero);

        private AppDbContext _context = null!;
        private Mock<TimeProvider> _timeProviderMock = null!;
        private EventsHandler _sut = null!;
        private DateTimeOffset _now;

        [SetUp]
        public void SetUp()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _timeProviderMock = new Mock<TimeProvider>();
            SetNow(_defaultNow);
            _sut = new EventsHandler(
                _context,
                new RecurrenceExpander(),
                _timeProviderMock.Object,
                Microsoft.Extensions.Options.Options.Create(new StudioTimeOptions { TimeZone = STUDIO_TIME_ZONE }));
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
            await _sut.CreateAsync(BuildSaveModel("Past Show", _now.AddDays(-5)), "admin@example.com");
            await _sut.CreateAsync(BuildSaveModel("Future Show", _now.AddDays(5)), "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Has.Count.EqualTo(1));
            Assert.That(response.Data![0].Title, Is.EqualTo("Future Show"));
        }

        [Test]
        public async Task GetUpcomingAsync_EventStartedEarlierToday_StillReturned()
        {
            await _sut.CreateAsync(BuildSaveModel("Morning Show", _now.AddHours(-3)), "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Has.Count.EqualTo(1));
            Assert.That(response.Data![0].Title, Is.EqualTo("Morning Show"));
        }

        [Test]
        public async Task GetUpcomingAsync_EventStartedThisMorningQueriedInEvening_StillReturned()
        {
            // 10 AM and 8 PM Central (CST, UTC-6); 8 PM local is already the next UTC day.
            DateTimeOffset tenAmLocal = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.FromHours(-6));
            DateTimeOffset eightPmLocal = new DateTimeOffset(2026, 11, 12, 20, 0, 0, TimeSpan.FromHours(-6));
            await _sut.CreateAsync(BuildSaveModel("Morning Show", tenAmLocal), "admin@example.com");
            SetNow(eightPmLocal);

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Has.Count.EqualTo(1));
            Assert.That(response.Data![0].Title, Is.EqualTo("Morning Show"));
        }

        [Test]
        public async Task GetUpcomingAsync_EventStartedYesterdayEveningLocal_NotReturned()
        {
            // 7 PM Central yesterday falls on today's UTC date, but it's not today for the studio.
            DateTimeOffset sevenPmYesterdayLocal = new DateTimeOffset(2026, 11, 11, 19, 0, 0, TimeSpan.FromHours(-6));
            DateTimeOffset eightAmLocal = new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.FromHours(-6));
            await _sut.CreateAsync(BuildSaveModel("Last Night Show", sevenPmYesterdayLocal), "admin@example.com");
            SetNow(eightAmLocal);

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Is.Empty);
        }

        [Test]
        public async Task GetUpcomingAsync_EventEndedEarlierToday_NotReturned()
        {
            EventSaveModel model = BuildSaveModel("Overnight Show", _now.AddDays(-1));
            model.EndDateTime = _now.AddHours(-3);
            await _sut.CreateAsync(model, "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetUpcomingAsync();

            Assert.That(response.Data, Is.Empty);
        }

        [Test]
        public async Task CreateAsync_SetsCreatedByEmail()
        {
            DataHandlerResponse<Guid> createResponse = await _sut.CreateAsync(BuildSaveModel("Show", _now.AddDays(1)), "owner@example.com");

            DataHandlerResponse<EventModel> detail = await _sut.GetByIdAsync(createResponse.Data);
            Assert.That(detail.IsSuccess, Is.True);
        }

        [Test]
        public async Task SetImageAsync_ReplacesExistingImage_ReturnsPreviousFileName()
        {
            DataHandlerResponse<Guid> createResponse = await _sut.CreateAsync(BuildSaveModel("Show", _now.AddDays(1)), "owner@example.com");
            await _sut.SetImageAsync(createResponse.Data, "first.jpg");

            DataHandlerResponse<string?> secondSet = await _sut.SetImageAsync(createResponse.Data, "second.jpg");

            Assert.That(secondSet.Data, Is.EqualTo("first.jpg"));
        }

        [Test]
        public async Task GetUpcomingAsync_WeeklyRecurringEvent_ExpandsMultipleOccurrences()
        {
            EventSaveModel model = BuildRecurringSaveModel(
                "Weekly Studio Night",
                _now.AddDays(1),
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
                _now.AddDays(1),
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
            DateTimeOffset start = _now.AddDays(1);
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
                _now.AddDays(-30),
                RecurrenceFrequency.Weekly,
                interval: 1);
            await _sut.CreateAsync(model, "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetOccurrencesAsync();

            Assert.That(response.Data!.Any(e => e.StartDateTime < _now), Is.True);
        }

        [Test]
        public async Task GetOccurrencesInRangeAsync_RecurringEvent_BoundedByCallerSuppliedRange()
        {
            EventSaveModel model = BuildRecurringSaveModel(
                "Long Series",
                _now.AddDays(-60),
                RecurrenceFrequency.Weekly,
                interval: 1);
            await _sut.CreateAsync(model, "admin@example.com");

            DateTimeOffset rangeStart = _now.AddDays(-14);
            DateTimeOffset rangeEnd = _now.AddDays(14);
            DataHandlerResponse<List<EventModel>> response = await _sut.GetOccurrencesInRangeAsync(rangeStart, rangeEnd);

            Assert.That(response.Data!.All(e => e.StartDateTime >= rangeStart && e.StartDateTime <= rangeEnd), Is.True);
            Assert.That(response.Data, Is.Not.Empty);
        }

        [Test]
        public async Task GetOccurrencesInRangeAsync_NonRecurringEventOutsideRange_Excluded()
        {
            await _sut.CreateAsync(BuildSaveModel("Far Future Show", _now.AddYears(1)), "admin@example.com");

            DataHandlerResponse<List<EventModel>> response = await _sut.GetOccurrencesInRangeAsync(
                _now, _now.AddDays(30));

            Assert.That(response.Data, Is.Empty);
        }

        [Test]
        public async Task UpdateAsync_TogglesRecurrenceBackToNone_ClearsIntervalAndEndDate()
        {
            DateTimeOffset start = _now.AddDays(1);
            EventSaveModel recurringModel = BuildRecurringSaveModel("Toggled Series", start, RecurrenceFrequency.Weekly, interval: 3, recurrenceEndDate: start.AddMonths(2));
            DataHandlerResponse<Guid> createResponse = await _sut.CreateAsync(recurringModel, "admin@example.com");

            EventSaveModel updatedModel = BuildSaveModel("Toggled Series", start);
            updatedModel.RecurrenceFrequency = RecurrenceFrequency.None;
            await _sut.UpdateAsync(createResponse.Data, updatedModel);

            DataHandlerResponse<EventModel> detail = await _sut.GetByIdAsync(createResponse.Data);
            Assert.That(detail.Data!.RecurrenceInterval, Is.EqualTo(1));
            Assert.That(detail.Data!.RecurrenceEndDate, Is.Null);
        }

        private void SetNow(DateTimeOffset now)
        {
            _now = now;
            _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(now.ToUniversalTime());
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
