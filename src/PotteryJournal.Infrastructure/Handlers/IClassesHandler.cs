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
        /// Gets every class availability rule, soonest anchor first.
        /// </summary>
        Task<DataHandlerResponse<List<ClassAvailabilityModel>>> GetAvailabilityRulesAsync();

        /// <summary>
        /// Creates a new class availability rule.
        /// </summary>
        /// <param name="model">The submitted class type, anchor date/time, and recurrence.</param>
        Task<DataHandlerResponse<Guid>> CreateAvailabilityRuleAsync(ClassAvailabilitySaveModel model);

        /// <summary>
        /// Deletes a class availability rule. Since occurrences are virtual, this removes the whole
        /// series -- existing <see cref="ClassBooking"/> rows it may have produced are unaffected.
        /// </summary>
        /// <param name="id">The availability rule's primary key.</param>
        Task<HandlerResponse> DeleteAvailabilityRuleAsync(Guid id);

        /// <summary>
        /// Computes every currently bookable class slot within the given range: every availability
        /// rule's occurrences, minus slots inside a blackout period, inside the minimum booking lead
        /// time, or already booked (Tentative or Confirmed) for that class type and time.
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
        /// Gets class bookings, optionally filtered by status, most recently requested first.
        /// </summary>
        /// <param name="status">When supplied, only bookings with this status are returned.</param>
        Task<DataHandlerResponse<List<ClassBookingModel>>> GetBookingsAsync(ClassBookingStatus? status);

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
