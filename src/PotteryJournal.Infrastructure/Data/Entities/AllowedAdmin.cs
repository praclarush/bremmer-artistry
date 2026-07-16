using System;

namespace PotteryJournal.Infrastructure.Data.Entities
{
    /// <summary>
    /// A Google account email permitted to sign in to the admin area.
    /// </summary>
    public class AllowedAdmin
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public DateTimeOffset AddedDate { get; set; }

        public string? AddedByEmail { get; set; }

        public bool IsActive { get; set; }
    }
}
