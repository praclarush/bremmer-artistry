using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for the managed reference-data lists (clay bodies, glazes, categories) that
    /// back the Pottery Journal's piece-edit dropdowns.
    /// </summary>
    public interface IReferenceDataHandler
    {
        /// <summary>
        /// Gets every clay body, ordered by name.
        /// </summary>
        Task<DataHandlerResponse<List<LookupItemModel>>> GetClayBodiesAsync();

        /// <summary>
        /// Gets every glaze, ordered by name.
        /// </summary>
        Task<DataHandlerResponse<List<LookupItemModel>>> GetGlazesAsync();

        /// <summary>
        /// Gets every category, ordered by name.
        /// </summary>
        Task<DataHandlerResponse<List<LookupItemModel>>> GetCategoriesAsync();

        /// <summary>
        /// Gets every collection, ordered by name, with its piece count and whether it's the one
        /// currently featured on the homepage.
        /// </summary>
        Task<DataHandlerResponse<List<CollectionModel>>> GetCollectionsAsync();

        /// <summary>
        /// Gets every class type, ordered by name.
        /// </summary>
        Task<DataHandlerResponse<List<ClassTypeModel>>> GetClassTypesAsync();

        /// <summary>
        /// Adds a new clay body. Fails if the name is blank or already exists.
        /// </summary>
        /// <param name="name">The clay body's display name.</param>
        Task<DataHandlerResponse<Guid>> AddClayBodyAsync(string name);

        /// <summary>
        /// Adds a new glaze. Fails if the name is blank or already exists.
        /// </summary>
        /// <param name="name">The glaze's display name.</param>
        Task<DataHandlerResponse<Guid>> AddGlazeAsync(string name);

        /// <summary>
        /// Adds a new category. Fails if the name is blank or already exists.
        /// </summary>
        /// <param name="name">The category's display name.</param>
        Task<DataHandlerResponse<Guid>> AddCategoryAsync(string name);

        /// <summary>
        /// Adds a new collection. Fails if the name is blank or already exists.
        /// </summary>
        /// <param name="name">The collection's display name.</param>
        Task<DataHandlerResponse<Guid>> AddCollectionAsync(string name);

        /// <summary>
        /// Adds a new class type. Fails if the name is blank, already exists, or maxCapacity is less than 1.
        /// </summary>
        /// <param name="name">The class type's display name.</param>
        /// <param name="maxCapacity">The maximum party size a single booking may request for this class type.</param>
        Task<DataHandlerResponse<Guid>> AddClassTypeAsync(string name, int maxCapacity);

        /// <summary>
        /// Updates a class type's maximum party size.
        /// </summary>
        /// <param name="id">The class type's primary key.</param>
        /// <param name="maxCapacity">The new maximum party size. Fails if less than 1.</param>
        Task<HandlerResponse> UpdateClassTypeCapacityAsync(Guid id, int maxCapacity);

        /// <summary>
        /// Removes a clay body. Any piece using it reverts to unspecified.
        /// </summary>
        /// <param name="id">The clay body's primary key.</param>
        Task<HandlerResponse> RemoveClayBodyAsync(Guid id);

        /// <summary>
        /// Removes a glaze. Fails if any glaze application still references it.
        /// </summary>
        /// <param name="id">The glaze's primary key.</param>
        Task<HandlerResponse> RemoveGlazeAsync(Guid id);

        /// <summary>
        /// Removes a category. Any piece using it reverts to uncategorized (and drops out of the
        /// Gallery, since Gallery grouping requires a category).
        /// </summary>
        /// <param name="id">The category's primary key.</param>
        Task<HandlerResponse> RemoveCategoryAsync(Guid id);

        /// <summary>
        /// Removes a collection. Any piece using it reverts to no collection.
        /// </summary>
        /// <param name="id">The collection's primary key.</param>
        Task<HandlerResponse> RemoveCollectionAsync(Guid id);

        /// <summary>
        /// Removes a class type.
        /// </summary>
        /// <param name="id">The class type's primary key.</param>
        Task<HandlerResponse> RemoveClassTypeAsync(Guid id);

        /// <summary>
        /// Seeds the default class types ("Wheel Throw", "Hand-Building") if the ClassTypes table is
        /// empty. No-ops otherwise, so it's safe to call on every startup.
        /// </summary>
        Task<HandlerResponse> EnsureSeedClassTypesAsync();

        /// <summary>
        /// Sets or clears whether a collection is featured on the homepage. Setting one collection
        /// featured automatically un-features every other collection, since at most one is shown at
        /// a time.
        /// </summary>
        /// <param name="id">The collection's primary key.</param>
        /// <param name="isFeatured">Whether this collection should become the featured one.</param>
        Task<HandlerResponse> SetCollectionFeaturedAsync(Guid id, bool isFeatured);
    }
}
