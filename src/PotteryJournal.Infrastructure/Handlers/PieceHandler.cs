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
    /// Business logic for the Pottery Journal piece catalog.
    /// </summary>
    public class PieceHandler : IPieceHandler
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="PieceHandler"/>.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public PieceHandler(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<PagedHandlerResponse<PieceSummaryModel>> GetSummariesPagedAsync(string? sortColumn, bool sortDescending, int pageNumber, int pageSize)
        {
            IQueryable<Piece> query = _context.Pieces
                .Include(p => p.Images)
                .Include(p => p.ClayBody)
                .Include(p => p.Category)
                .AsNoTracking();

            query = ApplySort(query, sortColumn, sortDescending);

            int totalRecords = await query.CountAsync();
            List<Piece> pieces = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<PieceSummaryModel> summaries = pieces.Select(ToSummaryModel).ToList();
            return new PagedHandlerResponse<PieceSummaryModel>(summaries, totalRecords, pageSize, pageNumber);
        }

        private static IQueryable<Piece> ApplySort(IQueryable<Piece> query, string? sortColumn, bool sortDescending)
        {
            switch (sortColumn)
            {
                case "title":
                    return sortDescending ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title);
                case "clay":
                    return sortDescending ? query.OrderByDescending(p => p.ClayBody!.Name) : query.OrderBy(p => p.ClayBody!.Name);
                case "category":
                    return sortDescending ? query.OrderByDescending(p => p.Category!.Name) : query.OrderBy(p => p.Category!.Name);
                case "gallery":
                    return sortDescending ? query.OrderByDescending(p => p.ShowInGallery) : query.OrderBy(p => p.ShowInGallery);
                case "started":
                    return sortDescending ? query.OrderByDescending(p => p.StartedDate) : query.OrderBy(p => p.StartedDate);
                case "number":
                default:
                    return sortDescending ? query.OrderByDescending(p => p.PieceNumber) : query.OrderBy(p => p.PieceNumber);
            }
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<PieceDetailModel>>> GetAllDetailsAsync(string? category)
        {
            DataHandlerResponse<List<PieceDetailModel>> response = new DataHandlerResponse<List<PieceDetailModel>>();

            // Public Pottery Journal endpoint only -- pieces with no glaze recorded yet aren't
            // shown outside the admin portal. Admin screens use GetByIdAsync/GetSummariesAsync
            // instead, which are unfiltered.
            IQueryable<Piece> query = _context.Pieces
                .Include(p => p.Notes)
                .Include(p => p.Images)
                .Include(p => p.GlazeApplications).ThenInclude(g => g.Glaze)
                .Include(p => p.ClayBody)
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.GlazeApplications.Any());

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category != null && p.Category.Name == category);
            }

            List<Piece> pieces = await query
                .OrderByDescending(p => p.PieceNumber)
                .ToListAsync();

            response.Data = pieces.Select(ToDetailModel).ToList();
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<GalleryCategoryModel>>> GetGalleryCategoriesAsync()
        {
            DataHandlerResponse<List<GalleryCategoryModel>> response = new DataHandlerResponse<List<GalleryCategoryModel>>();

            List<Piece> pieces = await _context.Pieces
                .Include(p => p.Images)
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.ShowInGallery && p.Category != null)
                .OrderByDescending(p => p.StartedDate)
                .ToListAsync();

            response.Data = pieces
                .GroupBy(p => p.Category!.Name)
                .Select(group => new GalleryCategoryModel
                {
                    Category = group.Key,
                    PieceCount = group.Count(),
                    RepresentativeImageFileName = group.First().Images
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.FileName)
                        .FirstOrDefault(),
                })
                .OrderBy(c => c.Category)
                .ToList();
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<GalleryPieceModel>>> GetGalleryPiecesAsync()
        {
            DataHandlerResponse<List<GalleryPieceModel>> response = new DataHandlerResponse<List<GalleryPieceModel>>();

            List<Piece> pieces = await _context.Pieces
                .Include(p => p.Images)
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.ShowInGallery && p.Category != null)
                .OrderByDescending(p => p.StartedDate)
                .ToListAsync();

            response.Data = pieces
                .Select(p => new GalleryPieceModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Category = p.Category!.Name,
                    ImageFileNames = p.Images
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.FileName)
                        .ToList(),
                })
                .ToList();
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<PieceDetailModel>> GetByNumberAsync(int pieceNumber)
        {
            Piece? piece = await _context.Pieces
                .Include(p => p.Notes)
                .Include(p => p.Images)
                .Include(p => p.GlazeApplications).ThenInclude(g => g.Glaze)
                .Include(p => p.ClayBody)
                .Include(p => p.Category)
                .Include(p => p.Collection)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PieceNumber == pieceNumber);

            return BuildDetailResponse(piece);
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<PieceDetailModel>> GetByIdAsync(Guid id)
        {
            Piece? piece = await _context.Pieces
                .Include(p => p.Notes)
                .Include(p => p.Images)
                .Include(p => p.GlazeApplications).ThenInclude(g => g.Glaze)
                .Include(p => p.ClayBody)
                .Include(p => p.Category)
                .Include(p => p.Collection)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            return BuildDetailResponse(piece);
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<FeaturedCollectionModel?>> GetFeaturedCollectionAsync()
        {
            DataHandlerResponse<FeaturedCollectionModel?> response = new DataHandlerResponse<FeaturedCollectionModel?>();

            Collection? featured = await _context.Collections.AsNoTracking().FirstOrDefaultAsync(c => c.IsFeaturedOnHomepage);
            if (featured is null)
            {
                response.IsSuccess = true;
                return response;
            }

            List<Piece> pieces = await _context.Pieces
                .Include(p => p.Images)
                .AsNoTracking()
                .Where(p => p.CollectionId == featured.Id && p.Images.Any())
                .OrderBy(p => p.PieceNumber)
                .ToListAsync();

            if (pieces.Count == 0)
            {
                response.IsSuccess = true;
                return response;
            }

            response.Data = new FeaturedCollectionModel
            {
                CollectionName = featured.Name,
                Pieces = pieces.Select(p => new FeaturedCollectionPieceModel
                {
                    Title = p.Title,
                    ImageFileName = p.Images.OrderBy(i => i.SortOrder).Select(i => i.FileName).First(),
                }).ToList(),
            };
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> CreateAsync(PieceSaveModel model, string createdByEmail)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();

            int nextPieceNumber = await _context.Pieces.AnyAsync()
                ? await _context.Pieces.MaxAsync(p => p.PieceNumber) + 1
                : 1;

            Piece piece = new Piece
            {
                PieceNumber = nextPieceNumber,
                CreatedDate = DateTimeOffset.UtcNow,
                CreatedByEmail = createdByEmail,
            };
            ApplySaveModel(piece, model);
            piece.Notes = BuildNotes(model.Notes);
            piece.GlazeApplications = BuildGlazeApplications(model.GlazeApplications);

            _context.Pieces.Add(piece);
            await _context.SaveChangesAsync();

            response.Data = piece.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> UpdateAsync(Guid id, PieceSaveModel model)
        {
            HandlerResponse response = new HandlerResponse();

            Piece? piece = await _context.Pieces
                .Include(p => p.Notes)
                .Include(p => p.GlazeApplications)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (piece is null)
            {
                response.AddError($"No piece was found with id {id}.");
                response.IsSuccess = false;
                return response;
            }

            ApplySaveModel(piece, model);

            _context.PieceNotes.RemoveRange(piece.Notes);
            _context.GlazeApplications.RemoveRange(piece.GlazeApplications);
            piece.Notes = BuildNotes(model.Notes);
            piece.GlazeApplications = BuildGlazeApplications(model.GlazeApplications);

            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> DeleteAsync(Guid id)
        {
            HandlerResponse response = new HandlerResponse();

            Piece? piece = await _context.Pieces.FirstOrDefaultAsync(p => p.Id == id);
            if (piece is null)
            {
                response.AddError($"No piece was found with id {id}.");
                response.IsSuccess = false;
                return response;
            }

            _context.Pieces.Remove(piece);
            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<Guid>> AddImageAsync(Guid pieceId, string fileName)
        {
            DataHandlerResponse<Guid> response = new DataHandlerResponse<Guid>();

            int nextSortOrder = await _context.PieceImages
                .Where(i => i.PieceId == pieceId)
                .Select(i => (int?)i.SortOrder)
                .MaxAsync() ?? -1;
            nextSortOrder++;

            PieceImage image = new PieceImage
            {
                PieceId = pieceId,
                FileName = fileName,
                SortOrder = nextSortOrder,
                CreatedDate = DateTimeOffset.UtcNow,
            };

            _context.PieceImages.Add(image);
            await _context.SaveChangesAsync();

            response.Data = image.Id;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<string>> RemoveImageAsync(Guid imageId)
        {
            DataHandlerResponse<string> response = new DataHandlerResponse<string>();

            PieceImage? image = await _context.PieceImages.FirstOrDefaultAsync(i => i.Id == imageId);
            if (image is null)
            {
                response.AddError($"No photo was found with id {imageId}.");
                response.IsSuccess = false;
                return response;
            }

            _context.PieceImages.Remove(image);
            await _context.SaveChangesAsync();

            response.Data = image.FileName;
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<HandlerResponse> ReorderImagesAsync(Guid pieceId, List<Guid> orderedImageIds)
        {
            HandlerResponse response = new HandlerResponse();

            List<PieceImage> images = await _context.PieceImages
                .Where(i => i.PieceId == pieceId)
                .ToListAsync();

            for (int position = 0; position < orderedImageIds.Count; position++)
            {
                PieceImage? image = images.FirstOrDefault(i => i.Id == orderedImageIds[position]);
                if (image is not null)
                {
                    image.SortOrder = position;
                }
            }

            await _context.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }

        private static void ApplySaveModel(Piece piece, PieceSaveModel model)
        {
            piece.Title = model.Title;
            piece.CategoryId = model.CategoryId;
            piece.ShowInGallery = model.ShowInGallery;
            piece.ClayBodyId = model.ClayBodyId;
            piece.CollectionId = model.CollectionId;
            piece.StartedDate = model.StartedDate;
            piece.FinishedDate = model.FinishedDate;
            piece.SizeText = model.SizeText;
            piece.WeightText = model.WeightText;
            piece.GlazeSummary = model.GlazeSummary;
            piece.AttachmentsText = model.AttachmentsText;
        }

        private static List<PieceNote> BuildNotes(List<PieceNoteModel> notes)
        {
            List<PieceNote> result = new List<PieceNote>();
            for (int i = 0; i < notes.Count; i++)
            {
                result.Add(new PieceNote
                {
                    Title = notes[i].Title,
                    NoteText = notes[i].NoteText,
                    SortOrder = i,
                });
            }

            return result;
        }

        private static List<GlazeApplication> BuildGlazeApplications(List<GlazeApplicationModel> glazeApplications)
        {
            return glazeApplications
                .Where(g => g.GlazeId.HasValue)
                .Select(g => new GlazeApplication
                {
                    Location = g.Location,
                    GlazeId = g.GlazeId!.Value,
                    Coats = g.Coats,
                }).ToList();
        }

        private static DataHandlerResponse<PieceDetailModel> BuildDetailResponse(Piece? piece)
        {
            DataHandlerResponse<PieceDetailModel> response = new DataHandlerResponse<PieceDetailModel>();

            if (piece is null)
            {
                response.AddError("No piece was found.");
                response.IsSuccess = false;
                return response;
            }

            response.Data = ToDetailModel(piece);
            response.IsSuccess = true;
            return response;
        }

        private static PieceSummaryModel ToSummaryModel(Piece piece)
        {
            return new PieceSummaryModel
            {
                Id = piece.Id,
                PieceNumber = piece.PieceNumber,
                Title = piece.Title,
                Clay = piece.ClayBody?.Name ?? "—",
                Category = piece.Category?.Name,
                ShowInGallery = piece.ShowInGallery,
                StartedDate = piece.StartedDate,
                PrimaryImageFileName = piece.Images.OrderBy(i => i.SortOrder).Select(i => i.FileName).FirstOrDefault(),
            };
        }

        private static PieceDetailModel ToDetailModel(Piece piece)
        {
            return new PieceDetailModel
            {
                Id = piece.Id,
                PieceNumber = piece.PieceNumber,
                Title = piece.Title,
                CategoryId = piece.CategoryId,
                Category = piece.Category?.Name,
                ShowInGallery = piece.ShowInGallery,
                ClayBodyId = piece.ClayBodyId,
                Clay = piece.ClayBody?.Name ?? "—",
                CollectionId = piece.CollectionId,
                Collection = piece.Collection?.Name,
                StartedDate = piece.StartedDate,
                FinishedDate = piece.FinishedDate,
                SizeText = piece.SizeText,
                WeightText = piece.WeightText,
                GlazeSummary = piece.GlazeSummary,
                AttachmentsText = piece.AttachmentsText,
                Notes = piece.Notes
                    .OrderBy(n => n.SortOrder)
                    .Select(n => new PieceNoteModel { Title = n.Title, NoteText = n.NoteText })
                    .ToList(),
                GlazeApplications = piece.GlazeApplications
                    .Select(g => new GlazeApplicationModel { Location = g.Location, GlazeId = g.GlazeId, GlazeName = g.Glaze?.Name ?? string.Empty, Coats = g.Coats })
                    .ToList(),
                Images = piece.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new PieceImageModel { Id = i.Id, FileName = i.FileName, SortOrder = i.SortOrder })
                    .ToList(),
            };
        }
    }
}
