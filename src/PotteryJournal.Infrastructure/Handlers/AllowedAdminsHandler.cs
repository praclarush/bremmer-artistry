using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for admin accounts and sign-in.
    /// </summary>
    public class AllowedAdminsHandler : IAllowedAdminsHandler
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<AllowedAdmin> _passwordHasher;

        /// <summary>
        /// Initializes a new instance of <see cref="AllowedAdminsHandler"/>.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="passwordHasher">Hashes and verifies admin account passwords.</param>
        public AllowedAdminsHandler(AppDbContext context, IPasswordHasher<AllowedAdmin> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<AllowedAdminModel>>> GetAllAsync()
        {
            DataHandlerResponse<List<AllowedAdminModel>> response = new DataHandlerResponse<List<AllowedAdminModel>>();

            List<AllowedAdmin> admins = await _context.AllowedAdmins
                .AsNoTracking()
                .OrderByDescending(a => a.AddedDate)
                .ToListAsync();

            response.Data = admins.Select(ToModel).ToList();
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<AllowedAdminModel>> ValidateCredentialsAsync(string email, string password)
        {
            DataHandlerResponse<AllowedAdminModel> response = new DataHandlerResponse<AllowedAdminModel>();

            string normalizedEmail = NormalizeEmail(email);
            AllowedAdmin? admin = await _context.AllowedAdmins
                .FirstOrDefaultAsync(a => a.Email == normalizedEmail && a.IsActive);

            if (admin is null)
            {
                response.AddError("Invalid email or password.");
                response.IsSuccess = false;
                return response;
            }

            PasswordVerificationResult verificationResult = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                response.AddError("Invalid email or password.");
                response.IsSuccess = false;
                return response;
            }

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                admin.PasswordHash = _passwordHasher.HashPassword(admin, password);
                await _context.SaveChangesAsync();
            }

            response.Data = ToModel(admin);
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddAsync(string email, string password, string? displayName, string? addedByEmail)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();

            string normalizedEmail = NormalizeEmail(email);
            bool alreadyExists = await _context.AllowedAdmins.AnyAsync(a => a.Email == normalizedEmail);
            if (alreadyExists)
            {
                response.AddError($"{normalizedEmail} already has an admin account.");
                response.IsSuccess = false;
                return response;
            }

            AllowedAdmin admin = new AllowedAdmin
            {
                Email = normalizedEmail,
                DisplayName = displayName,
                AddedDate = DateTimeOffset.UtcNow,
                AddedByEmail = addedByEmail,
                IsActive = true,
            };
            admin.PasswordHash = _passwordHasher.HashPassword(admin, password);

            _context.AllowedAdmins.Add(admin);
            await _context.SaveChangesAsync();

            response.Data = admin.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveAsync(Guid id)
        {
            HandlerResponse response = new HandlerResponse();

            AllowedAdmin? admin = await _context.AllowedAdmins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin is null)
            {
                response.AddError($"No admin account was found with id {id}.");
                response.IsSuccess = false;
                return response;
            }

            _context.AllowedAdmins.Remove(admin);
            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> ChangePasswordAsync(Guid id, string newPassword)
        {
            HandlerResponse response = new HandlerResponse();

            AllowedAdmin? admin = await _context.AllowedAdmins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin is null)
            {
                response.AddError($"No admin account was found with id {id}.");
                response.IsSuccess = false;
                return response;
            }

            admin.PasswordHash = _passwordHasher.HashPassword(admin, newPassword);
            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> EnsureBootstrapAdminAsync(string bootstrapEmail, string bootstrapPassword)
        {
            HandlerResponse response = new HandlerResponse();

            bool anyAdminsExist = await _context.AllowedAdmins.AnyAsync();
            if (anyAdminsExist)
            {
                response.IsSuccess = true;
                return response;
            }

            AllowedAdmin admin = new AllowedAdmin
            {
                Email = NormalizeEmail(bootstrapEmail),
                AddedDate = DateTimeOffset.UtcNow,
                IsActive = true,
            };
            admin.PasswordHash = _passwordHasher.HashPassword(admin, bootstrapPassword);

            _context.AllowedAdmins.Add(admin);
            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static AllowedAdminModel ToModel(AllowedAdmin admin)
        {
            return new AllowedAdminModel
            {
                Id = admin.Id,
                Email = admin.Email,
                DisplayName = admin.DisplayName,
                AddedDate = admin.AddedDate,
                AddedByEmail = admin.AddedByEmail,
                IsActive = admin.IsActive,
            };
        }
    }
}
