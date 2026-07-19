using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for class scheduling: blackout periods, admin-defined availability windows,
    /// the computed public list of bookable slots, and customer booking requests.
    /// </summary>
    public class ClassesHandler : IClassesHandler
    {
        // Classes are always fixed 2-hour segments -- not a per-availability-rule setting.
        private static readonly TimeSpan _classDuration = TimeSpan.FromHours(2);

        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IAdminSettingsHandler _adminSettingsHandler;

        /// <summary>
        /// Initializes a new instance of <see cref="ClassesHandler"/>.
        /// </summary>
        public ClassesHandler(
            AppDbContext context,
            IEmailSender emailSender,
            IAdminSettingsHandler adminSettingsHandler)
        {
            _context = context;
            _emailSender = emailSender;
            _adminSettingsHandler = adminSettingsHandler;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<BlackoutPeriodModel>>> GetBlackoutPeriodsAsync()
        {
            List<BlackoutPeriod> blackouts = await _context.BlackoutPeriods.AsNoTracking().OrderBy(b => b.StartDateTime).ToListAsync();
            return new DataHandlerResponse<List<BlackoutPeriodModel>>
            {
                Data = blackouts.Select(ToModel).ToList(),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddBlackoutPeriodAsync(BlackoutPeriodSaveModel model)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();

            if (model.EndDateTime <= model.StartDateTime)
            {
                response.AddError("End must be after start.");
                response.IsSuccess = false;
                return response;
            }

            BlackoutPeriod blackout = new BlackoutPeriod
            {
                StartDateTime = model.StartDateTime.ToUniversalTime(),
                EndDateTime = model.EndDateTime.ToUniversalTime(),
                Reason = model.Reason,
                CreatedDate = DateTimeOffset.UtcNow,
            };

            _context.BlackoutPeriods.Add(blackout);
            await _context.SaveChangesAsync();

            response.Data = blackout.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveBlackoutPeriodAsync(Guid id)
        {
            BlackoutPeriod? blackout = await _context.BlackoutPeriods.FirstOrDefaultAsync(b => b.Id == id);
            if (blackout is null)
            {
                return HandlerResponse.NotFound("blackout period", id);
            }

            _context.BlackoutPeriods.Remove(blackout);
            await _context.SaveChangesAsync();
            return new HandlerResponse { IsSuccess = true };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<ClassAvailabilityModel>>> GetAvailabilityRulesAsync()
        {
            List<ClassAvailability> rules = await _context.ClassAvailabilities
                .AsNoTracking()
                .Include(a => a.ClassType)
                .OrderBy(a => a.ClassType!.Name)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            return new DataHandlerResponse<List<ClassAvailabilityModel>>
            {
                Data = rules.Select(ToModel).ToList(),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<ClassAvailabilityModel>> GetAvailabilityRuleByIdAsync(Guid id)
        {
            ClassAvailability? rule = await _context.ClassAvailabilities
                .AsNoTracking()
                .Include(a => a.ClassType)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (rule is null)
            {
                return DataHandlerResponse<ClassAvailabilityModel>.NotFound("class availability rule", id);
            }

            return new DataHandlerResponse<ClassAvailabilityModel>
            {
                Data = ToModel(rule),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> CreateAvailabilityRuleAsync(ClassAvailabilitySaveModel model)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();

            ClassAvailability rule = new ClassAvailability { CreatedDate = DateTimeOffset.UtcNow };
            HandlerResponse validation = await ValidateAndApplySaveModelAsync(rule, model);
            if (!validation.IsSuccess)
            {
                response.Concat(validation);
                response.IsSuccess = false;
                return response;
            }

            _context.ClassAvailabilities.Add(rule);
            await _context.SaveChangesAsync();

            response.Data = rule.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> UpdateAvailabilityRuleAsync(Guid id, ClassAvailabilitySaveModel model)
        {
            ClassAvailability? rule = await _context.ClassAvailabilities.FirstOrDefaultAsync(a => a.Id == id);
            if (rule is null)
            {
                return HandlerResponse.NotFound("class availability rule", id);
            }

            HandlerResponse validation = await ValidateAndApplySaveModelAsync(rule, model);
            if (!validation.IsSuccess)
            {
                return validation;
            }

            await _context.SaveChangesAsync();
            return new HandlerResponse { IsSuccess = true };
        }

        private async Task<HandlerResponse> ValidateAndApplySaveModelAsync(ClassAvailability rule, ClassAvailabilitySaveModel model)
        {
            HandlerResponse response = new HandlerResponse();

            bool classTypeExists = await _context.ClassTypes.AsNoTracking().AnyAsync(c => c.Id == model.ClassTypeId);
            if (!classTypeExists)
            {
                response.AddError("That class type no longer exists.");
                response.IsSuccess = false;
                return response;
            }

            ClassAvailabilityDays daysOfWeek = ClassAvailabilityDays.None;
            foreach (ClassAvailabilityDays day in model.DaysOfWeek)
            {
                daysOfWeek |= day;
            }

            if (daysOfWeek == ClassAvailabilityDays.None)
            {
                response.AddError("Select at least one day of the week.");
                response.IsSuccess = false;
                return response;
            }

            TimeSpan lastStartTime = model.RepeatsMultipleTimesPerDay ? model.LastStartTime : model.StartTime;
            int intervalHours = model.RepeatsMultipleTimesPerDay ? Math.Max(1, model.IntervalHours) : 1;

            if (lastStartTime < model.StartTime)
            {
                response.AddError("The last class start time must be at or after the start time.");
                response.IsSuccess = false;
                return response;
            }

            rule.ClassTypeId = model.ClassTypeId;
            rule.DaysOfWeek = daysOfWeek;
            rule.StartTime = model.StartTime;
            rule.LastStartTime = lastStartTime;
            rule.IntervalHours = intervalHours;

            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> DeleteAvailabilityRuleAsync(Guid id)
        {
            ClassAvailability? rule = await _context.ClassAvailabilities.FirstOrDefaultAsync(a => a.Id == id);
            if (rule is null)
            {
                return HandlerResponse.NotFound("class availability rule", id);
            }

            _context.ClassAvailabilities.Remove(rule);
            await _context.SaveChangesAsync();
            return new HandlerResponse { IsSuccess = true };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<ClassSlotModel>>> GetAvailableSlotsAsync(DateTimeOffset from, DateTimeOffset to)
        {
            DataHandlerResponse<List<ClassSlotModel>> response = new DataHandlerResponse<List<ClassSlotModel>>();

            DataHandlerResponse<AdminSettingsModel> settingsResponse = await _adminSettingsHandler.GetAsync();
            int minimumLeadDays = settingsResponse.Data?.MinimumBookingLeadDays ?? 0;
            DateTimeOffset earliestBookable = DateTimeOffset.UtcNow.AddDays(minimumLeadDays);

            List<ClassAvailability> rules = await _context.ClassAvailabilities
                .AsNoTracking()
                .Include(a => a.ClassType)
                .ToListAsync();
            List<BlackoutPeriod> blackouts = await _context.BlackoutPeriods.AsNoTracking().ToListAsync();
            HashSet<(Guid ClassTypeId, DateTimeOffset StartDateTime)> bookedSlots = await GetActiveBookedSlotsAsync(from, to);

            List<ClassSlotModel> slots = new List<ClassSlotModel>();
            foreach (ClassAvailability rule in rules)
            {
                if (rule.ClassType is null || rule.DaysOfWeek == ClassAvailabilityDays.None)
                {
                    continue;
                }

                foreach (DateTimeOffset occurrenceStart in ExpandWeeklyRule(rule, from, to))
                {
                    if (occurrenceStart < earliestBookable)
                    {
                        continue;
                    }

                    DateTimeOffset occurrenceEnd = occurrenceStart + _classDuration;
                    if (blackouts.Any(b => occurrenceStart < b.EndDateTime && occurrenceEnd > b.StartDateTime))
                    {
                        continue;
                    }

                    if (bookedSlots.Contains((rule.ClassTypeId, occurrenceStart)))
                    {
                        continue;
                    }

                    slots.Add(new ClassSlotModel
                    {
                        ClassTypeId = rule.ClassTypeId,
                        ClassTypeName = rule.ClassType.Name,
                        MaxCapacity = rule.ClassType.MaxCapacity,
                        StartDateTime = occurrenceStart,
                        EndDateTime = occurrenceEnd,
                    });
                }
            }

            response.Data = slots.OrderBy(s => s.StartDateTime).ToList();
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> CreateBookingAsync(ClassBookingSaveModel model)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();

            (HandlerResponse validation, ClassBooking? booking, string classTypeName) =
                await ValidateAndBuildBookingAsync(model, enforceMinimumLeadTime: true);
            if (!validation.IsSuccess || booking is null)
            {
                response.Concat(validation);
                response.IsSuccess = false;
                return response;
            }

            booking.Status = ClassBookingStatus.Tentative;
            _context.ClassBookings.Add(booking);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The partial unique index caught a race with a concurrent booking of the same slot
                // that the AnyAsync check above missed.
                response.AddError("That slot was just booked by someone else. Please pick another time.");
                response.IsSuccess = false;
                return response;
            }

            response.Data = booking.Id;
            response.IsSuccess = true;

            DataHandlerResponse<AdminSettingsModel> settingsResponse = await _adminSettingsHandler.GetAsync();
            string? recipient = settingsResponse.Data?.NotificationRecipientEmail;
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                string subject = $"New class booking request: {classTypeName}";
                string body =
                    $"{booking.CustomerName} ({booking.CustomerEmail}) requested {classTypeName} on {booking.StartDateTime:f} for a party of {booking.PartySize}." +
                    $"\nPhone: {booking.CustomerPhone}" +
                    (string.IsNullOrWhiteSpace(booking.Message) ? string.Empty : $"\nMessage: {booking.Message}") +
                    "\n\nReview and approve or decline it from /admin/classes/bookings.";
                HandlerResponse emailResponse = await _emailSender.SendAsync(recipient, subject, body);
                if (!emailResponse.IsSuccess)
                {
                    response.AddWarning($"The booking was recorded, but the studio notification email could not be sent: {string.Join(" ", emailResponse.Errors)}");
                }
            }

            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> CreateManualBookingAsync(ClassBookingSaveModel model)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();

            (HandlerResponse validation, ClassBooking? booking, string classTypeName) =
                await ValidateAndBuildBookingAsync(model, enforceMinimumLeadTime: false);
            if (!validation.IsSuccess || booking is null)
            {
                response.Concat(validation);
                response.IsSuccess = false;
                return response;
            }

            booking.Status = ClassBookingStatus.Confirmed;
            booking.DecisionDate = DateTimeOffset.UtcNow;
            _context.ClassBookings.Add(booking);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                response.AddError("That slot is already booked. Please pick another time.");
                response.IsSuccess = false;
                return response;
            }

            response.Data = booking.Id;
            response.IsSuccess = true;

            HandlerResponse emailResponse = await SendBookingConfirmationEmailAsync(booking, classTypeName);
            if (!emailResponse.IsSuccess)
            {
                response.AddWarning($"The booking was scheduled, but the customer confirmation email could not be sent: {string.Join(" ", emailResponse.Errors)}");
            }

            return response;
        }

        /// <summary>
        /// Validates a booking request (required fields, capacity, blackout, and slot-conflict
        /// checks shared by both the public and admin-manual booking paths) and, on success, builds
        /// -- but does not persist -- the resulting <see cref="ClassBooking"/>. Minimum lead time is
        /// only enforced for public self-service submissions; an admin manually scheduling a call-in
        /// booking is a deliberate override of that guardrail, not a bypass of the physical
        /// constraints (capacity, blackout, double-booking) below it.
        /// </summary>
        private async Task<(HandlerResponse Response, ClassBooking? Booking, string ClassTypeName)> ValidateAndBuildBookingAsync(
            ClassBookingSaveModel model, bool enforceMinimumLeadTime)
        {
            HandlerResponse response = new HandlerResponse();

            string normalizedName = model.CustomerName?.Trim() ?? string.Empty;
            string normalizedEmail = model.CustomerEmail?.Trim() ?? string.Empty;
            string normalizedPhone = model.CustomerPhone?.Trim() ?? string.Empty;
            if (normalizedName.Length == 0)
            {
                response.AddError("The customer's name is required.");
            }

            if (normalizedEmail.Length == 0)
            {
                response.AddError("The customer's email is required.");
            }

            if (normalizedPhone.Length == 0)
            {
                response.AddError("The customer's phone number is required.");
            }

            if (model.PartySize < 1)
            {
                response.AddError("Party size must be at least 1.");
            }

            ClassType? classType = await _context.ClassTypes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == model.ClassTypeId);
            if (classType is null)
            {
                response.AddError("That class type no longer exists.");
            }
            else if (model.PartySize > classType.MaxCapacity)
            {
                response.AddError($"That class allows a maximum group size of {classType.MaxCapacity}.");
            }

            DateTimeOffset startDateTime = model.StartDateTime.ToUniversalTime();
            DateTimeOffset endDateTime = startDateTime + _classDuration;

            if (enforceMinimumLeadTime)
            {
                DataHandlerResponse<AdminSettingsModel> settingsResponse = await _adminSettingsHandler.GetAsync();
                int minimumLeadDays = settingsResponse.Data?.MinimumBookingLeadDays ?? 0;
                if (startDateTime < DateTimeOffset.UtcNow.AddDays(minimumLeadDays))
                {
                    response.AddError($"Bookings must be made at least {minimumLeadDays} day{(minimumLeadDays == 1 ? string.Empty : "s")} in advance.");
                }
            }

            bool isBlackedOut = await _context.BlackoutPeriods
                .AsNoTracking()
                .AnyAsync(b => startDateTime < b.EndDateTime && endDateTime > b.StartDateTime);
            if (isBlackedOut)
            {
                response.AddError("That date and time is not available for booking.");
            }

            bool slotAlreadyBooked = await _context.ClassBookings
                .AsNoTracking()
                .AnyAsync(b => b.ClassTypeId == model.ClassTypeId && b.StartDateTime == startDateTime && b.Status != ClassBookingStatus.Declined);
            if (slotAlreadyBooked)
            {
                response.AddError("That slot is already booked. Please pick another time.");
            }

            if (response.Errors.Count > 0)
            {
                response.IsSuccess = false;
                return (response, null, string.Empty);
            }

            ClassBooking booking = new ClassBooking
            {
                ClassTypeId = model.ClassTypeId,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                CustomerName = normalizedName,
                CustomerEmail = normalizedEmail,
                CustomerPhone = normalizedPhone,
                PartySize = model.PartySize,
                Message = model.Message,
                CreatedDate = DateTimeOffset.UtcNow,
            };

            response.IsSuccess = true;
            return (response, booking, classType!.Name);
        }

        private async Task<HandlerResponse> SendBookingConfirmationEmailAsync(ClassBooking booking, string classTypeName)
        {
            string subject = $"Your {classTypeName} booking is confirmed";
            string body =
                $"Hi {booking.CustomerName},\n\n" +
                $"Your booking for {classTypeName} on {booking.StartDateTime:f} (party of {booking.PartySize}) is confirmed. We look forward to seeing you!";
            return await _emailSender.SendAsync(booking.CustomerEmail, subject, body);
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<ClassBookingModel>>> GetBookingsAsync(ClassBookingStatus? status)
        {
            IQueryable<ClassBooking> query = _context.ClassBookings.AsNoTracking().Include(b => b.ClassType);
            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            List<ClassBooking> bookings = await query.OrderByDescending(b => b.CreatedDate).ToListAsync();
            return new DataHandlerResponse<List<ClassBookingModel>>
            {
                Data = bookings.Select(ToModel).ToList(),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<ClassBookingModel>>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to)
        {
            List<ClassBooking> bookings = await _context.ClassBookings
                .AsNoTracking()
                .Include(b => b.ClassType)
                .Where(b => b.Status != ClassBookingStatus.Declined && b.StartDateTime >= from && b.StartDateTime <= to)
                .OrderBy(b => b.StartDateTime)
                .ToListAsync();

            return new DataHandlerResponse<List<ClassBookingModel>>
            {
                Data = bookings.Select(ToModel).ToList(),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> ApproveBookingAsync(Guid id)
        {
            HandlerResponse response = new HandlerResponse();

            ClassBooking? booking = await _context.ClassBookings.Include(b => b.ClassType).FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null)
            {
                return HandlerResponse.NotFound("class booking", id);
            }

            if (booking.Status != ClassBookingStatus.Tentative)
            {
                response.AddError("Only a tentative booking can be approved.");
                response.IsSuccess = false;
                return response;
            }

            booking.Status = ClassBookingStatus.Confirmed;
            booking.DecisionDate = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            response.IsSuccess = true;

            HandlerResponse emailResponse = await SendBookingConfirmationEmailAsync(booking, booking.ClassType?.Name ?? "class");
            if (!emailResponse.IsSuccess)
            {
                response.AddWarning($"The booking was confirmed, but the customer confirmation email could not be sent: {string.Join(" ", emailResponse.Errors)}");
            }

            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> DeclineBookingAsync(Guid id)
        {
            ClassBooking? booking = await _context.ClassBookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null)
            {
                return HandlerResponse.NotFound("class booking", id);
            }

            booking.Status = ClassBookingStatus.Declined;
            booking.DecisionDate = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return new HandlerResponse { IsSuccess = true };
        }

        private static List<DateTimeOffset> ExpandWeeklyRule(ClassAvailability rule, DateTimeOffset from, DateTimeOffset to)
        {
            List<DateTimeOffset> occurrences = new List<DateTimeOffset>();

            DateTime day = from.UtcDateTime.Date;
            DateTime lastDay = to.UtcDateTime.Date;
            while (day <= lastDay)
            {
                ClassAvailabilityDays dayFlag = (ClassAvailabilityDays)(1 << (int)day.DayOfWeek);
                if (rule.DaysOfWeek.HasFlag(dayFlag))
                {
                    TimeSpan occurrenceTime = rule.StartTime;
                    while (occurrenceTime <= rule.LastStartTime)
                    {
                        occurrences.Add(new DateTimeOffset(day, TimeSpan.Zero) + occurrenceTime);
                        occurrenceTime += TimeSpan.FromHours(rule.IntervalHours);
                    }
                }

                day = day.AddDays(1);
            }

            return occurrences;
        }

        private async Task<HashSet<(Guid ClassTypeId, DateTimeOffset StartDateTime)>> GetActiveBookedSlotsAsync(DateTimeOffset from, DateTimeOffset to)
        {
            List<ClassBooking> activeBookings = await _context.ClassBookings
                .AsNoTracking()
                .Where(b => b.Status != ClassBookingStatus.Declined && b.StartDateTime >= from && b.StartDateTime <= to)
                .ToListAsync();

            return activeBookings.Select(b => (b.ClassTypeId, b.StartDateTime)).ToHashSet();
        }

        private static BlackoutPeriodModel ToModel(BlackoutPeriod blackout)
        {
            return new BlackoutPeriodModel
            {
                Id = blackout.Id,
                StartDateTime = blackout.StartDateTime,
                EndDateTime = blackout.EndDateTime,
                Reason = blackout.Reason,
            };
        }

        private static ClassAvailabilityModel ToModel(ClassAvailability rule)
        {
            return new ClassAvailabilityModel
            {
                Id = rule.Id,
                ClassTypeId = rule.ClassTypeId,
                ClassTypeName = rule.ClassType?.Name ?? string.Empty,
                DaysOfWeek = rule.DaysOfWeek,
                StartTime = rule.StartTime,
                LastStartTime = rule.LastStartTime,
                IntervalHours = rule.IntervalHours,
            };
        }

        private static ClassBookingModel ToModel(ClassBooking booking)
        {
            return new ClassBookingModel
            {
                Id = booking.Id,
                ClassTypeId = booking.ClassTypeId,
                ClassTypeName = booking.ClassType?.Name ?? string.Empty,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                CustomerName = booking.CustomerName,
                CustomerEmail = booking.CustomerEmail,
                CustomerPhone = booking.CustomerPhone,
                PartySize = booking.PartySize,
                Message = booking.Message,
                Status = booking.Status,
                CreatedDate = booking.CreatedDate,
                DecisionDate = booking.DecisionDate,
            };
        }
    }
}
