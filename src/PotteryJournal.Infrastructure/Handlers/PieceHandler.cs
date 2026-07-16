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
        public async Task<DataHandlerResponse<List<PieceSummaryModel>>> GetSummariesAsync(string? category)
        {
            DataHandlerResponse<List<PieceSummaryModel>> response = new DataHandlerResponse<List<PieceSummaryModel>>();

            IQueryable<Piece> query = _context.Pieces
                .Include(p => p.Images)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            List<Piece> pieces = await query
                .OrderByDescending(p => p.PieceNumber)
                .ToListAsync();

            response.Data = pieces.Select(ToSummaryModel).ToList();
            response.IsSuccess = true;
            return response;
        }

        /// <inheritdoc />
        public async Task<DataHandlerResponse<List<PieceDetailModel>>> GetAllDetailsAsync(string? category)
        {
            DataHandlerResponse<List<PieceDetailModel>> response = new DataHandlerResponse<List<PieceDetailModel>>();

            IQueryable<Piece> query = _context.Pieces
                .Include(p => p.Notes)
                .Include(p => p.Images)
                .Include(p => p.GlazeApplications)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
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
                .AsNoTracking()
                .Where(p => p.Category != null && p.Category != string.Empty)
                .OrderByDescending(p => p.StartedDate)
                .ToListAsync();

            response.Data = pieces
                .GroupBy(p => p.Category!)
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
        public async Task<DataHandlerResponse<PieceDetailModel>> GetByNumberAsync(int pieceNumber)
        {
            Piece? piece = await _context.Pieces
                .Include(p => p.Notes)
                .Include(p => p.Images)
                .Include(p => p.GlazeApplications)
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
                .Include(p => p.GlazeApplications)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            return BuildDetailResponse(piece);
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
            piece.Category = model.Category;
            piece.Clay = model.Clay;
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
            return glazeApplications.Select(g => new GlazeApplication
            {
                Location = g.Location,
                GlazeName = g.GlazeName,
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
                Clay = piece.Clay,
                Category = piece.Category,
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
                Category = piece.Category,
                Clay = piece.Clay,
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
                    .Select(g => new GlazeApplicationModel { Location = g.Location, GlazeName = g.GlazeName, Coats = g.Coats })
                    .ToList(),
                Images = piece.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new PieceImageModel { Id = i.Id, FileName = i.FileName, SortOrder = i.SortOrder })
                    .ToList(),
            };
        }
    }
}
