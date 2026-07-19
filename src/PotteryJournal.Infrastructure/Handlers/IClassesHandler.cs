using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for class scheduling: blackout periods, admin-defined availability windows,
    /// the computed public list of bookable slots, and customer booking requests.
    /// </summary>
    public interface IClassesHandler
    {
        /// <summary>
        /// Gets every blackout period, soonest first.
        /// </summary>
        Task<DataHandlerResponse<List<BlackoutPeriodModel>>> GetBlackoutPeriodsAsync();

        /// <summary>
        /// Adds a new blackout period.
        /// </summary>
        /// <param name="model">The submitted start/end/reason.</param>
        Task<DataHandlerResponse<Guid>> AddBlackoutPeriodAsync(BlackoutPeriodSaveModel model);

        /// <summary>
        /// Removes a blackout period.
        /// </summary>
        /// <param name="id">The blackout period's primary key.</param>
        Task<HandlerResponse> RemoveBlackoutPeriodAsync(Guid id);

        /// <summary>
        /// Gets every class availability rule, grouped by class type then start time.
        /// </summary>
        Task<DataHandlerResponse<List<ClassAvailabilityModel>>> GetAvailabilityRulesAsync();

        /// <summary>
        /// Gets a single class availability rule by id, for the admin edit form.
        /// </summary>
        /// <param name="id">The availability rule's primary key.</param>
        Task<DataHandlerResponse<ClassAvailabilityModel>> GetAvailabilityRuleByIdAsync(Guid id);

        /// <summary>
        /// Creates a new class availability rule: a class type, the weekdays it's offered on, a
        /// start time, and optionally a repeat interval and last start time to offer multiple
        /// classes per matching day. The rule recurs indefinitely -- <see cref="BlackoutPeriod"/> is
        /// the mechanism for excluding specific dates/times.
        /// </summary>
        /// <param name="model">The submitted class type, days of week, and start time(s).</param>
        Task<DataHandlerResponse<Guid>> CreateAvailabilityRuleAsync(ClassAvailabilitySaveModel model);

        /// <summary>
        /// Updates an existing class availability rule in place -- since occurrences are virtual,
        /// this immediately changes what's bookable going forward without touching any <see
        /// cref="ClassBooking"/> rows already made against the rule's previous shape.
        /// </summary>
        /// <param name="id">The availability rule's primary key.</param>
        /// <param name="model">The submitted class type, days of week, and start time(s).</param>
        Task<HandlerResponse> UpdateAvailabilityRuleAsync(Guid id, ClassAvailabilitySaveModel model);

        /// <summary>
        /// Deletes a class availability rule. Since occurrences are virtual, this removes the whole
        /// weekly pattern -- existing <see cref="ClassBooking"/> rows it may have produced are
        /// unaffected.
        /// </summary>
        /// <param name="id">The availability rule's primary key.</param>
        Task<HandlerResponse> DeleteAvailabilityRuleAsync(Guid id);

        /// <summary>
        /// Computes every currently bookable class slot within the given range: every availability
        /// rule's weekly occurrences, minus slots inside a blackout period, inside the minimum
        /// booking lead time, or already booked (Tentative or Confirmed) for that class type and
        /// time.
        /// </summary>
        /// <param name="from">Inclusive start of the range to search.</param>
        /// <param name="to">Inclusive end of the range to search.</param>
        Task<DataHandlerResponse<List<ClassSlotModel>>> GetAvailableSlotsAsync(DateTimeOffset from, DateTimeOffset to);

        /// <summary>
        /// Submits a new class booking request as <see cref="ClassBookingStatus.Tentative"/>, after
        /// re-validating lead time, blackout, party size, and slot availability server-side. Sends a
        /// notification email to the studio's configured address.
        /// </summary>
        /// <param name="model">The submitted booking request.</param>
        Task<DataHandlerResponse<Guid>> CreateBookingAsync(ClassBookingSaveModel model);

        /// <summary>
        /// Creates a booking directly as <see cref="ClassBookingStatus.Confirmed"/>, for an admin
        /// scheduling a class on a customer's behalf (e.g. a phone call) -- skips the minimum
        /// booking lead time (a public self-service guardrail that doesn't apply to a deliberate
        /// admin override), but still enforces capacity, blackout periods, and the one-booking-per-
        /// slot constraint. Emails the customer a confirmation directly; there's no separate approval
        /// step since the admin creating it has already decided to book it.
        /// </summary>
        /// <param name="model">The submitted booking details.</param>
        Task<DataHandlerResponse<Guid>> CreateManualBookingAsync(ClassBookingSaveModel model);

        /// <summary>
        /// Gets class bookings, optionally filtered by status, most recently requested first.
        /// </summary>
        /// <param name="status">When supplied, only bookings with this status are returned.</param>
        Task<DataHandlerResponse<List<ClassBookingModel>>> GetBookingsAsync(ClassBookingStatus? status);

        /// <summary>
        /// Gets non-declined class bookings (Tentative and Confirmed) within the given range, for
        /// the admin calendar.
        /// </summary>
        /// <param name="from">Inclusive start of the range to search.</param>
        /// <param name="to">Inclusive end of the range to search.</param>
        Task<DataHandlerResponse<List<ClassBookingModel>>> GetBookingsInRangeAsync(DateTimeOffset from, DateTimeOffset to);

        /// <summary>
        /// Approves a tentative booking, marking it <see cref="ClassBookingStatus.Confirmed"/> and
        /// sending the customer a confirmation email.
        /// </summary>
        /// <param name="id">The booking's primary key.</param>
        Task<HandlerResponse> ApproveBookingAsync(Guid id);

        /// <summary>
        /// Declines a tentative booking, marking it <see cref="ClassBookingStatus.Declined"/> and
        /// freeing the slot for a new request.
        /// </summary>
        /// <param name="id">The booking's primary key.</param>
        Task<HandlerResponse> DeclineBookingAsync(Guid id);
    }
}
