using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for admin accounts and sign-in.
    /// </summary>
    public interface IAllowedAdminsHandler
    {
        /// <summary>
        /// Gets every admin account, most recently added first.
        /// </summary>
        Task<DataHandlerResponse<List<AllowedAdminModel>>> GetAllAsync();

        /// <summary>
        /// Validates an email/password combination against an active admin account.
        /// </summary>
        /// <param name="email">The submitted sign-in email.</param>
        /// <param name="password">The submitted plain-text password.</param>
        /// <returns>A response whose <c>Data</c> is the matched admin on success.</returns>
        Task<DataHandlerResponse<AllowedAdminModel>> ValidateCredentialsAsync(string email, string password);

        /// <summary>
        /// Creates a new admin account with the given initial password.
        /// </summary>
        /// <param name="email">The new admin's sign-in email.</param>
        /// <param name="password">The new admin's initial plain-text password.</param>
        /// <param name="displayName">An optional display name for this admin.</param>
        /// <param name="addedByEmail">The email of the admin adding this entry, if added via the UI.</param>
        Task<DataHandlerResponse<Guid>> AddAsync(string email, string password, string? displayName, string? addedByEmail);

        /// <summary>
        /// Removes an admin account.
        /// </summary>
        /// <param name="id">The admin account's primary key.</param>
        Task<HandlerResponse> RemoveAsync(Guid id);

        /// <summary>
        /// Changes an admin account's password.
        /// </summary>
        /// <param name="id">The admin account's primary key.</param>
        /// <param name="newPassword">The new plain-text password.</param>
        Task<HandlerResponse> ChangePasswordAsync(Guid id, string newPassword);

        /// <summary>
        /// Seeds the admin list with the given bootstrap email/password if it's currently empty.
        /// </summary>
        /// <param name="bootstrapEmail">The email to seed as the first admin.</param>
        /// <param name="bootstrapPassword">The initial password for the seeded admin.</param>
        Task<HandlerResponse> EnsureBootstrapAdminAsync(string bootstrapEmail, string bootstrapPassword);
    }
}
