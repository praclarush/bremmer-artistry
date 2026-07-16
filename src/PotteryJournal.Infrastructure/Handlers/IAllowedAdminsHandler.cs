using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for the admin sign-in allow-list.
    /// </summary>
    public interface IAllowedAdminsHandler
    {
        /// <summary>
        /// Gets every allow-list entry, most recently added first.
        /// </summary>
        Task<DataHandlerResponse<List<AllowedAdminModel>>> GetAllAsync();

        /// <summary>
        /// Checks whether the given email is an active allow-list entry.
        /// </summary>
        /// <param name="email">The Google account email to check.</param>
        Task<DataHandlerResponse<bool>> IsAllowedAsync(string email);

        /// <summary>
        /// Adds a new email to the allow-list.
        /// </summary>
        /// <param name="email">The Google account email to allow.</param>
        /// <param name="displayName">An optional display name for this admin.</param>
        /// <param name="addedByEmail">The email of the admin adding this entry, if added via the UI.</param>
        Task<DataHandlerResponse<Guid>> AddAsync(string email, string? displayName, string? addedByEmail);

        /// <summary>
        /// Removes an email from the allow-list.
        /// </summary>
        /// <param name="id">The allow-list entry's primary key.</param>
        Task<HandlerResponse> RemoveAsync(Guid id);

        /// <summary>
        /// Seeds the allow-list with the given bootstrap email if the allow-list is currently empty.
        /// </summary>
        /// <param name="bootstrapEmail">The email to seed as the first admin.</param>
        Task<HandlerResponse> EnsureBootstrapAdminAsync(string bootstrapEmail);
    }
}
