using System;

namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// An allow-listed admin email, as read through <see cref="Handlers.IAllowedAdminsHandler"/>.
    /// </summary>
    public class AllowedAdminModel
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public DateTimeOffset AddedDate { get; set; }

        public string? AddedByEmail { get; set; }

        public bool IsActive { get; set; }
    }
}
