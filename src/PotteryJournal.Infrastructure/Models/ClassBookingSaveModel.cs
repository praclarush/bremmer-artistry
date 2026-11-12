using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A customer's class booking request, submitted from the public Classes page.
    /// </summary>
    public class ClassBookingSaveModel
    {
        public Guid ClassTypeId { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public int PartySize { get; set; } = 1;

        public string? Message { get; set; }
    }
}
