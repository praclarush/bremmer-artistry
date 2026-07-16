using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for the admin sign-in allow-list.
    /// </summary>
    public class AllowedAdminsHandler : IAllowedAdminsHandler
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="AllowedAdminsHandler"/>.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public AllowedAdminsHandler(AppDbContext context)
        {
            _context = context;
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
        public async Task<DataHandlerResponse<bool>> IsAllowedAsync(string email)
        {
            DataHandlerResponse<bool> response = new DataHandlerResponse<bool>();

            string normalizedEmail = NormalizeEmail(email);
            response.Data = await _context.AllowedAdmins
                .AsNoTracking()
                .AnyAsync(a => a.Email == normalizedEmail && a.IsActive);
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddAsync(string email, string? displayName, string? addedByEmail)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();

            string normalizedEmail = NormalizeEmail(email);
            bool alreadyExists = await _context.AllowedAdmins.AnyAsync(a => a.Email == normalizedEmail);
            if (alreadyExists)
            {
                response.AddError($"{normalizedEmail} is already on the allow-list.");
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
                response.AddError($"No allow-list entry was found with id {id}.");
                response.IsSuccess = false;
                return response;
            }

            _context.AllowedAdmins.Remove(admin);
            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> EnsureBootstrapAdminAsync(string bootstrapEmail)
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
