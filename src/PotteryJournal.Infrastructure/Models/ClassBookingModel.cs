using System;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A customer's class booking request, as reviewed by an admin.
    /// </summary>
    public class ClassBookingModel
    {
        public Guid Id { get; set; }

        public Guid ClassTypeId { get; set; }

        public string ClassTypeName { get; set; } = string.Empty;

        public DateTimeOffset StartDateTime { get; set; }

        public DateTimeOffset EndDateTime { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public int PartySize { get; set; } = 1;

        public string? Message { get; set; }

        public ClassBookingStatus Status { get; set; }

        public DateTimeOffset CreatedDate { get; set; }

        public DateTimeOffset? DecisionDate { get; set; }
    }
}
