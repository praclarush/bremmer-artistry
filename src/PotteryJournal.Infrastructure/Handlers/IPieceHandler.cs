using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for the Pottery Journal piece catalog.
    /// </summary>
    public interface IPieceHandler
    {
        /// <summary>
        /// Gets one page of piece summaries for the admin piece list, sorted by the given column.
        /// </summary>
        /// <param name="sortColumn">One of "number", "title", "clay", "category", "gallery", "started". Unrecognized or null sorts by piece number.</param>
        /// <param name="sortDescending">Whether to sort descending instead of ascending.</param>
        /// <param name="pageNumber">The 1-based page number to return.</param>
        /// <param name="pageSize">The number of pieces per page.</param>
        Task<PagedHandlerResponse<PieceSummaryModel>> GetSummariesPagedAsync(string? sortColumn, bool sortDescending, int pageNumber, int pageSize);

        /// <summary>
        /// Gets the full worksheet detail for every piece, newest first, optionally filtered to a
        /// single category. Used by the Pottery Journal page's single data fetch, which needs both
        /// grid and worksheet fields for every piece up front.
        /// </summary>
        /// <param name="category">When supplied, only pieces tagged with this category are returned.</param>
        Task<DataHandlerResponse<List<PieceDetailModel>>> GetAllDetailsAsync(string? category);

        /// <summary>
        /// Gets one Gallery tile per distinct category present on any piece curated for the
        /// Gallery (<see cref="Models.PieceSaveModel.ShowInGallery"/>), each with a representative
        /// image from the most recently started piece in that category.
        /// </summary>
        Task<DataHandlerResponse<List<GalleryCategoryModel>>> GetGalleryCategoriesAsync();

        /// <summary>
        /// Gets every piece curated for the Gallery, with all of its photos, for the Gallery page's
        /// category drill-down grid and lightbox. Independent of the Pottery Journal's own data --
        /// only pieces with <see cref="Models.PieceSaveModel.ShowInGallery"/> set are included.
        /// </summary>
        Task<DataHandlerResponse<List<GalleryPieceModel>>> GetGalleryPiecesAsync();

        /// <summary>
        /// Gets the collection currently featured on the homepage and one representative photo per
        /// piece in it, for the rotating display next to the hero. Returns a null <c>Data</c> when
        /// no collection is featured, or the featured collection has no pieces with a photo.
        /// </summary>
        Task<DataHandlerResponse<FeaturedCollectionModel?>> GetFeaturedCollectionAsync();

        /// <summary>
        /// Gets the full worksheet detail for a piece by its human-facing project number.
        /// </summary>
        /// <param name="pieceNumber">The piece's display number.</param>
        Task<DataHandlerResponse<PieceDetailModel>> GetByNumberAsync(int pieceNumber);

        /// <summary>
        /// Gets the full worksheet detail for a piece by its surrogate key, for admin editing.
        /// </summary>
        /// <param name="id">The piece's primary key.</param>
        Task<DataHandlerResponse<PieceDetailModel>> GetByIdAsync(Guid id);

        /// <summary>
        /// Creates a new piece, assigning it the next sequential piece number.
        /// </summary>
        /// <param name="model">The fields submitted from the admin create form.</param>
        /// <param name="createdByEmail">The email of the admin creating the piece.</param>
        Task<DataHandlerResponse<Guid>> CreateAsync(PieceSaveModel model, string createdByEmail);

        /// <summary>
        /// Updates an existing piece's fields, replacing its notes and glaze applications.
        /// </summary>
        /// <param name="id">The piece's primary key.</param>
        /// <param name="model">The fields submitted from the admin edit form.</param>
        Task<HandlerResponse> UpdateAsync(Guid id, PieceSaveModel model);

        /// <summary>
        /// Deletes a piece and its notes, glaze applications, and image records. Does not delete
        /// the underlying image files -- callers must do that via <see cref="Services.IImageStorageService"/>.
        /// </summary>
        /// <param name="id">The piece's primary key.</param>
        Task<HandlerResponse> DeleteAsync(Guid id);

        /// <summary>
        /// Records a newly uploaded photo against a piece, appending it to the end of the photo order.
        /// </summary>
        /// <param name="pieceId">The piece the photo belongs to.</param>
        /// <param name="fileName">The stored file name, as returned by <see cref="Services.IImageStorageService"/>.</param>
        Task<DataHandlerResponse<Guid>> AddImageAsync(Guid pieceId, string fileName);

        /// <summary>
        /// Removes a photo record from a piece.
        /// </summary>
        /// <param name="imageId">The photo's primary key.</param>
        /// <returns>A response whose <c>Data</c> is the removed file name, for the caller to delete from disk.</returns>
        Task<DataHandlerResponse<string>> RemoveImageAsync(Guid imageId);

        /// <summary>
        /// Reorders a piece's photos to match the given sequence of photo ids.
        /// </summary>
        /// <param name="pieceId">The piece whose photos are being reordered.</param>
        /// <param name="orderedImageIds">The photo ids in their new display order.</param>
        Task<HandlerResponse> ReorderImagesAsync(Guid pieceId, List<Guid> orderedImageIds);
    }
}
