using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
            return await AddLookupAsync(
                _context.ClayBodies,
                name,
                normalized => _context.ClayBodies.AsNoTracking().AnyAsync(c => c.Name == normalized),
                normalized => new ClayBody { Name = normalized },
                c => c.Id);
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddGlazeAsync(string name)
        {
            return await AddLookupAsync(
                _context.Glazes,
                name,
                normalized => _context.Glazes.AsNoTracking().AnyAsync(g => g.Name == normalized),
                normalized => new Glaze { Name = normalized },
                g => g.Id);
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddCategoryAsync(string name)
        {
            return await AddLookupAsync(
                _context.Categories,
                name,
                normalized => _context.Categories.AsNoTracking().AnyAsync(c => c.Name == normalized),
                normalized => new Category { Name = normalized },
                c => c.Id);
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddCollectionAsync(string name)
        {
            return await AddLookupAsync(
                _context.Collections,
                name,
                normalized => _context.Collections.AsNoTracking().AnyAsync(c => c.Name == normalized),
                normalized => new Collection { Name = normalized },
                c => c.Id);
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveClayBodyAsync(Guid id)
        {
            return await RemoveLookupAsync(_context.ClayBodies, "clay body", id, c => c.Id == id);
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveGlazeAsync(Guid id)
        {
            return await RemoveLookupAsync(_context.Glazes, "glaze", id, g => g.Id == id, async glaze =>
            {
                int usageCount = await _context.GlazeApplications.CountAsync(g => g.GlazeId == glaze.Id);
                if (usageCount == 0)
                {
                    return null;
                }

                HandlerResponse inUseResponse = new HandlerResponse();
                inUseResponse.AddError($"\"{glaze.Name}\" is still used by {usageCount} glaze application{(usageCount == 1 ? "" : "s")} and can't be deleted.");
                inUseResponse.IsSuccess = false;
                return inUseResponse;
            });
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveCategoryAsync(Guid id)
        {
            return await RemoveLookupAsync(_context.Categories, "category", id, c => c.Id == id);
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> RemoveCollectionAsync(Guid id)
        {
            return await RemoveLookupAsync(_context.Collections, "collection", id, c => c.Id == id);
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> SetCollectionFeaturedAsync(Guid id, bool isFeatured)
        {
            Collection? collection = await _context.Collections.FirstOrDefaultAsync(c => c.Id == id);
            if (collection is null)
            {
                return HandlerResponse.NotFound("collection", id);
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

        /// <summary>
        /// Shared implementation behind the Add*Async methods: normalizes the name, rejects a
        /// duplicate, then adds and saves the new entity.
        /// </summary>
        /// <param name="dbSet">The DbSet the new entity is added to.</param>
        /// <param name="name">The raw, unnormalized name submitted by the caller.</param>
        /// <param name="existsByNameAsync">Checks whether an entity with the normalized name already exists.</param>
        /// <param name="factory">Builds the new entity from the normalized name.</param>
        /// <param name="idSelector">Reads the new entity's id after it has been saved.</param>
        private async Task<DataHandlerResponse<Guid>> AddLookupAsync<TEntity>(
            DbSet<TEntity> dbSet,
            string name,
            Func<string, Task<bool>> existsByNameAsync,
            Func<string, TEntity> factory,
            Func<TEntity, Guid> idSelector)
            where TEntity : class
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();
            if (!TryNormalizeName(name, out string normalized, response))
            {
                return response;
            }

            if (await existsByNameAsync(normalized))
            {
                response.AddError($"\"{normalized}\" already exists.");
                response.IsSuccess = false;
                return response;
            }

            TEntity entity = factory(normalized);
            dbSet.Add(entity);
            await _context.SaveChangesAsync();

            response.Data = idSelector(entity);
            response.IsSuccess = true;
            return response;
        }

        /// <summary>
        /// Shared implementation behind the Remove*Async methods: finds the entity, optionally
        /// blocks removal when it's still in use, then removes and saves.
        /// </summary>
        /// <param name="dbSet">The DbSet the entity is removed from.</param>
        /// <param name="kind">A human-readable name for the entity type, used in the not-found error.</param>
        /// <param name="id">The id of the entity to remove.</param>
        /// <param name="idPredicate">Matches the entity with the given id.</param>
        /// <param name="inUseCheckAsync">When supplied, returns a failure response if the entity can't be removed yet (e.g. still referenced elsewhere), or null if removal may proceed.</param>
        private async Task<HandlerResponse> RemoveLookupAsync<TEntity>(
            DbSet<TEntity> dbSet,
            string kind,
            Guid id,
            Expression<Func<TEntity, bool>> idPredicate,
            Func<TEntity, Task<HandlerResponse?>>? inUseCheckAsync = null)
            where TEntity : class
        {
            TEntity? entity = await dbSet.FirstOrDefaultAsync(idPredicate);
            if (entity is null)
            {
                return HandlerResponse.NotFound(kind, id);
            }

            if (inUseCheckAsync is not null)
            {
                HandlerResponse? inUseResponse = await inUseCheckAsync(entity);
                if (inUseResponse is not null)
                {
                    return inUseResponse;
                }
            }

            dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return new HandlerResponse { IsSuccess = true };
        }
    }
}
