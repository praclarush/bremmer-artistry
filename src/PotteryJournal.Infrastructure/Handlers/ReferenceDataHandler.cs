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
    /// Business logic for the managed reference-data lists (clay bodies, glazes, categories).
    /// </summary>
    public class ReferenceDataHandler : IReferenceDataHandler
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="ReferenceDataHandler"/>.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public ReferenceDataHandler(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<LookupItemModel>>> GetClayBodiesAsync()
        {
            List<ClayBody> clayBodies = await _context.ClayBodies.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            return new DataHandlerResponse<List<LookupItemModel>>
            {
                Data = clayBodies.Select(c => new LookupItemModel { Id = c.Id, Name = c.Name }).ToList(),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<LookupItemModel>>> GetGlazesAsync()
        {
            List<Glaze> glazes = await _context.Glazes.AsNoTracking().OrderBy(g => g.Name).ToListAsync();
            return new DataHandlerResponse<List<LookupItemModel>>
            {
                Data = glazes.Select(g => new LookupItemModel { Id = g.Id, Name = g.Name }).ToList(),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<LookupItemModel>>> GetCategoriesAsync()
        {
            List<Category> categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            return new DataHandlerResponse<List<LookupItemModel>>
            {
                Data = categories.Select(c => new LookupItemModel { Id = c.Id, Name = c.Name }).ToList(),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<CollectionModel>>> GetCollectionsAsync()
        {
            List<Collection> collections = await _context.Collections.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            Dictionary<Guid, int> pieceCounts = await _context.Pieces
                .Where(p => p.CollectionId != null)
                .GroupBy(p => p.CollectionId!.Value)
                .Select(g => new { CollectionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CollectionId, x => x.Count);

            return new DataHandlerResponse<List<CollectionModel>>
            {
                Data = collections.Select(c => new CollectionModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsFeaturedOnHomepage = c.IsFeaturedOnHomepage,
                    PieceCount = pieceCounts.GetValueOrDefault(c.Id),
                }).ToList(),
                IsSuccess = true,
            };
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddClayBodyAsync(string name)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();
            if (!TryNormalizeName(name, out string normalized, response))
            {
                return response;
            }

            if (await _context.ClayBodies.AsNoTracking().AnyAsync(c => c.Name == normalized))
            {
                response.AddError($"\"{normalized}\" already exists.");
                response.IsSuccess = false;
                return response;
            }

            ClayBody clayBody = new ClayBody { Name = normalized };
            _context.ClayBodies.Add(clayBody);
            await _context.SaveChangesAsync();

            response.Data = clayBody.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddGlazeAsync(string name)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();
            if (!TryNormalizeName(name, out string normalized, response))
            {
                return response;
            }

            if (await _context.Glazes.AsNoTracking().AnyAsync(g => g.Name == normalized))
            {
                response.AddError($"\"{normalized}\" already exists.");
                response.IsSuccess = false;
                return response;
            }

            Glaze glaze = new Glaze { Name = normalized };
            _context.Glazes.Add(glaze);
            await _context.SaveChangesAsync();

            response.Data = glaze.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddCategoryAsync(string name)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();
            if (!TryNormalizeName(name, out string normalized, response))
            {
                return response;
            }

            if (await _context.Categories.AsNoTracking().AnyAsync(c => c.Name == normalized))
            {
                response.AddError($"\"{normalized}\" already exists.");
                response.IsSuccess = false;
                return response;
            }

            Category category = new Category { Name = normalized };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            response.Data = category.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddCollectionAsync(string name)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();
            if (!TryNormalizeName(name, out string normalized, response))
            {
                return response;
            }

            if (await _context.Collections.AsNoTracking().AnyAsync(c => c.Name == normalized))
            {
                response.AddError($"\"{normalized}\" already exists.");
                response.IsSuccess = false;
                return response;
            }

            Collection collection = new Collection { Name = normalized };
            _context.Collections.Add(collection);
            await _context.SaveChangesAsync();

            response.Data = collection.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveClayBodyAsync(Guid id)
        {
            ClayBody? clayBody = await _context.ClayBodies.FirstOrDefaultAsync(c => c.Id == id);
            if (clayBody is null)
            {
                return NotFoundResponse("clay body", id);
            }

            _context.ClayBodies.Remove(clayBody);
            await _context.SaveChangesAsync();
            return new HandlerResponse { IsSuccess = true };
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveGlazeAsync(Guid id)
        {
            Glaze? glaze = await _context.Glazes.FirstOrDefaultAsync(g => g.Id == id);
            if (glaze is null)
            {
                return NotFoundResponse("glaze", id);
            }

            int usageCount = await _context.GlazeApplications.CountAsync(g => g.GlazeId == id);
            if (usageCount > 0)
            {
                HandlerResponse inUseResponse = new HandlerResponse();
                inUseResponse.AddError($"\"{glaze.Name}\" is still used by {usageCount} glaze application{(usageCount == 1 ? "" : "s")} and can't be deleted.");
                inUseResponse.IsSuccess = false;
                return inUseResponse;
            }

            _context.Glazes.Remove(glaze);
            await _context.SaveChangesAsync();
            return new HandlerResponse { IsSuccess = true };
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveCategoryAsync(Guid id)
        {
            Category? category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category is null)
            {
                return NotFoundResponse("category", id);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return new HandlerResponse { IsSuccess = true };
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveCollectionAsync(Guid id)
        {
            Collection? collection = await _context.Collections.FirstOrDefaultAsync(c => c.Id == id);
            if (collection is null)
            {
                return NotFoundResponse("collection", id);
            }

            _context.Collections.Remove(collection);
            await _context.SaveChangesAsync();
            return new HandlerResponse { IsSuccess = true };
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> SetCollectionFeaturedAsync(Guid id, bool isFeatured)
        {
            Collection? collection = await _context.Collections.FirstOrDefaultAsync(c => c.Id == id);
            if (collection is null)
            {
                return NotFoundResponse("collection", id);
            }

            if (isFeatured)
            {
                List<Collection> currentlyFeatured = await _context.Collections
                    .Where(c => c.IsFeaturedOnHomepage && c.Id != id)
                    .ToListAsync();
                foreach (Collection other in currentlyFeatured)
                {
                    other.IsFeaturedOnHomepage = false;
                }
            }

            collection.IsFeaturedOnHomepage = isFeatured;
            await _context.SaveChangesAsync();
            return new HandlerResponse { IsSuccess = true };
        }

        private static bool TryNormalizeName(string name, out string normalized, HandlerResponse response)
        {
            normalized = name?.Trim() ?? string.Empty;
            if (normalized.Length == 0)
            {
                response.AddError("A name is required.");
                response.IsSuccess = false;
                return false;
            }

            return true;
        }

        private static HandlerResponse NotFoundResponse(string kind, Guid id)
        {
            HandlerResponse response = new HandlerResponse();
            response.AddError($"No {kind} was found with id {id}.");
            response.IsSuccess = false;
            return response;
        }
    }
}
