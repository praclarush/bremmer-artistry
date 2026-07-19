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
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Tests.Handlers
{
    [TestFixture]
    public class ClassesHandlerTests
    {
        private AppDbContext _context = null!;
        private ClassesHandler _sut = null!;
        private AdminSettingsHandler _adminSettingsHandler = null!;
        private Mock<IEmailSender> _emailSenderMock = null!;
        private Guid _wheelThrowId;

        [SetUp]
        public async Task SetUp()
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

            _sut = new ClassesHandler(_context, new RecurrenceExpander(), _emailSenderMock.Object, _adminSettingsHandler);

            await _adminSettingsHandler.UpdateAsync(new AdminSettingsModel
            {
                NotificationRecipientEmail = "studio@example.com",
                MinimumBookingLeadDays = 2,
            });

            ClassType wheelThrow = new ClassType { Name = "Wheel Throw", MaxCapacity = 4 };
            _context.ClassTypes.Add(wheelThrow);
            await _context.SaveChangesAsync();
            _wheelThrowId = wheelThrow.Id;
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
        public async Task GetAvailableSlotsAsync_WeeklyRecurringRule_ExpandsMultipleSlots()
        {
            await _sut.CreateAvailabilityRuleAsync(new ClassAvailabilitySaveModel
            {
                ClassTypeId = _wheelThrowId,
                StartDateTime = DateTimeOffset.UtcNow.AddDays(5),
                RecurrenceFrequency = RecurrenceFrequency.Weekly,
                RecurrenceInterval = 1,
            });

            DataHandlerResponse<List<ClassSlotModel>> response = await _sut.GetAvailableSlotsAsync(
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

            Assert.That(response.Data, Has.Count.GreaterThan(1));
            Assert.That(response.Data!.All(s => s.ClassTypeName == "Wheel Throw"), Is.True);
        }

        [Test]
        public async Task GetAvailableSlotsAsync_SlotInsideMinimumLeadTime_Excluded()
        {
            await _sut.CreateAvailabilityRuleAsync(new ClassAvailabilitySaveModel
            {
                ClassTypeId = _wheelThrowId,
                StartDateTime = DateTimeOffset.UtcNow.AddHours(6),
                RecurrenceFrequency = RecurrenceFrequency.None,
            });

            DataHandlerResponse<List<ClassSlotModel>> response = await _sut.GetAvailableSlotsAsync(
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

            Assert.That(response.Data, Is.Empty);
        }

        [Test]
        public async Task GetAvailableSlotsAsync_SlotInsideBlackoutPeriod_Excluded()
        {
            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);
            await _sut.CreateAvailabilityRuleAsync(new ClassAvailabilitySaveModel
            {
                ClassTypeId = _wheelThrowId,
                StartDateTime = slotStart,
                RecurrenceFrequency = RecurrenceFrequency.None,
            });
            await _sut.AddBlackoutPeriodAsync(new BlackoutPeriodSaveModel
            {
                StartDateTime = slotStart.AddHours(-1),
                EndDateTime = slotStart.AddHours(3),
            });

            DataHandlerResponse<List<ClassSlotModel>> response = await _sut.GetAvailableSlotsAsync(
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

            Assert.That(response.Data, Is.Empty);
        }

        [Test]
        public async Task GetAvailableSlotsAsync_AlreadyBookedSlot_Excluded()
        {
            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);
            await _sut.CreateAvailabilityRuleAsync(new ClassAvailabilitySaveModel
            {
                ClassTypeId = _wheelThrowId,
                StartDateTime = slotStart,
                RecurrenceFrequency = RecurrenceFrequency.None,
            });
            await _sut.CreateBookingAsync(BuildBookingModel(slotStart));

            DataHandlerResponse<List<ClassSlotModel>> response = await _sut.GetAvailableSlotsAsync(
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

            Assert.That(response.Data, Is.Empty);
        }

        [Test]
        public async Task CreateBookingAsync_ValidRequest_CreatesTentativeBookingAndNotifiesStudio()
        {
            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);

            DataHandlerResponse<Guid> response = await _sut.CreateBookingAsync(BuildBookingModel(slotStart));

            Assert.That(response.IsSuccess, Is.True);
            ClassBooking? booking = await _context.ClassBookings.FirstOrDefaultAsync(b => b.Id == response.Data);
            Assert.That(booking!.Status, Is.EqualTo(ClassBookingStatus.Tentative));
            _emailSenderMock.Verify(e => e.SendAsync("studio@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task CreateBookingAsync_PartySizeExceedsCapacity_ReturnsFailure()
        {
            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);
            ClassBookingSaveModel model = BuildBookingModel(slotStart);
            model.PartySize = 10;

            DataHandlerResponse<Guid> response = await _sut.CreateBookingAsync(model);

            Assert.That(response.IsSuccess, Is.False);
        }

        [Test]
        public async Task CreateBookingAsync_InsideMinimumLeadTime_ReturnsFailure()
        {
            ClassBookingSaveModel model = BuildBookingModel(DateTimeOffset.UtcNow.AddHours(6));

            DataHandlerResponse<Guid> response = await _sut.CreateBookingAsync(model);

            Assert.That(response.IsSuccess, Is.False);
        }

        [Test]
        public async Task CreateBookingAsync_SlotAlreadyActivelyBooked_ReturnsFailure()
        {
            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);
            await _sut.CreateBookingAsync(BuildBookingModel(slotStart));

            DataHandlerResponse<Guid> secondResponse = await _sut.CreateBookingAsync(BuildBookingModel(slotStart));

            Assert.That(secondResponse.IsSuccess, Is.False);
        }

        [Test]
        public async Task CreateBookingAsync_SameSlotDifferentClassType_BothSucceed()
        {
            ClassType handBuilding = new ClassType { Name = "Hand-Building", MaxCapacity = 4 };
            _context.ClassTypes.Add(handBuilding);
            await _context.SaveChangesAsync();

            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);
            DataHandlerResponse<Guid> firstResponse = await _sut.CreateBookingAsync(BuildBookingModel(slotStart));
            ClassBookingSaveModel secondModel = BuildBookingModel(slotStart);
            secondModel.ClassTypeId = handBuilding.Id;

            DataHandlerResponse<Guid> secondResponse = await _sut.CreateBookingAsync(secondModel);

            Assert.That(firstResponse.IsSuccess, Is.True);
            Assert.That(secondResponse.IsSuccess, Is.True);
        }

        [Test]
        public async Task CreateBookingAsync_PreviouslyDeclinedSlot_CanBeRebooked()
        {
            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);
            DataHandlerResponse<Guid> firstResponse = await _sut.CreateBookingAsync(BuildBookingModel(slotStart));
            await _sut.DeclineBookingAsync(firstResponse.Data);

            DataHandlerResponse<Guid> secondResponse = await _sut.CreateBookingAsync(BuildBookingModel(slotStart));

            Assert.That(secondResponse.IsSuccess, Is.True);
        }

        [Test]
        public async Task ApproveBookingAsync_TentativeBooking_ConfirmsAndEmailsCustomer()
        {
            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);
            DataHandlerResponse<Guid> createResponse = await _sut.CreateBookingAsync(BuildBookingModel(slotStart));

            HandlerResponse approveResponse = await _sut.ApproveBookingAsync(createResponse.Data);

            Assert.That(approveResponse.IsSuccess, Is.True);
            ClassBooking? booking = await _context.ClassBookings.FirstOrDefaultAsync(b => b.Id == createResponse.Data);
            Assert.That(booking!.Status, Is.EqualTo(ClassBookingStatus.Confirmed));
            _emailSenderMock.Verify(e => e.SendAsync("customer@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task ApproveBookingAsync_AlreadyConfirmed_ReturnsFailure()
        {
            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);
            DataHandlerResponse<Guid> createResponse = await _sut.CreateBookingAsync(BuildBookingModel(slotStart));
            await _sut.ApproveBookingAsync(createResponse.Data);

            HandlerResponse secondApproveResponse = await _sut.ApproveBookingAsync(createResponse.Data);

            Assert.That(secondApproveResponse.IsSuccess, Is.False);
        }

        [Test]
        public async Task DeclineBookingAsync_TentativeBooking_MarksDeclined()
        {
            DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddDays(5);
            DataHandlerResponse<Guid> createResponse = await _sut.CreateBookingAsync(BuildBookingModel(slotStart));

            HandlerResponse declineResponse = await _sut.DeclineBookingAsync(createResponse.Data);

            Assert.That(declineResponse.IsSuccess, Is.True);
            ClassBooking? booking = await _context.ClassBookings.FirstOrDefaultAsync(b => b.Id == createResponse.Data);
            Assert.That(booking!.Status, Is.EqualTo(ClassBookingStatus.Declined));
        }

        [Test]
        public async Task GetBookingsInRangeAsync_ExcludesDeclinedAndOutOfRangeBookings()
        {
            DateTimeOffset inRangeSlot = DateTimeOffset.UtcNow.AddDays(5);
            DateTimeOffset outOfRangeSlot = DateTimeOffset.UtcNow.AddDays(60);
            DataHandlerResponse<Guid> inRangeBooking = await _sut.CreateBookingAsync(BuildBookingModel(inRangeSlot));
            await _sut.CreateBookingAsync(BuildBookingModel(outOfRangeSlot));
            DataHandlerResponse<Guid> declinedBooking = await _sut.CreateBookingAsync(BuildBookingModel(inRangeSlot.AddHours(3)));
            await _sut.DeclineBookingAsync(declinedBooking.Data);

            DataHandlerResponse<List<ClassBookingModel>> response = await _sut.GetBookingsInRangeAsync(
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(10));

            Assert.That(response.Data, Has.Count.EqualTo(1));
            Assert.That(response.Data![0].Id, Is.EqualTo(inRangeBooking.Data));
        }

        [Test]
        public async Task GetBookingsAsync_FilteredByStatus_ReturnsOnlyMatching()
        {
            DateTimeOffset firstSlot = DateTimeOffset.UtcNow.AddDays(5);
            DateTimeOffset secondSlot = DateTimeOffset.UtcNow.AddDays(6);
            DataHandlerResponse<Guid> firstResponse = await _sut.CreateBookingAsync(BuildBookingModel(firstSlot));
            await _sut.CreateBookingAsync(BuildBookingModel(secondSlot));
            await _sut.ApproveBookingAsync(firstResponse.Data);

            DataHandlerResponse<List<ClassBookingModel>> tentativeResponse = await _sut.GetBookingsAsync(ClassBookingStatus.Tentative);
            DataHandlerResponse<List<ClassBookingModel>> confirmedResponse = await _sut.GetBookingsAsync(ClassBookingStatus.Confirmed);

            Assert.That(tentativeResponse.Data, Has.Count.EqualTo(1));
            Assert.That(confirmedResponse.Data, Has.Count.EqualTo(1));
        }

        private ClassBookingSaveModel BuildBookingModel(DateTimeOffset startDateTime)
        {
            return new ClassBookingSaveModel
            {
                ClassTypeId = _wheelThrowId,
                StartDateTime = startDateTime,
                CustomerName = "Jane Doe",
                CustomerEmail = "customer@example.com",
                PartySize = 2,
            };
        }
    }
}
