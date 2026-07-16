using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Constants;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Pieces
{
    public class EditModel : PageModel
    {
        private readonly IPieceHandler _pieceHandler;
        private readonly IImageStorageService _imageStorageService;

        public EditModel(IPieceHandler pieceHandler, IImageStorageService imageStorageService)
        {
            _pieceHandler = pieceHandler;
            _imageStorageService = imageStorageService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid? Id { get; set; }

        [BindProperty]
        public PieceSaveModel Piece { get; set; } = new PieceSaveModel();

        public int? PieceNumber { get; private set; }

        public List<PieceImageModel> Images { get; private set; } = new List<PieceImageModel>();

        public List<string> ExistingCategories { get; private set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadExistingCategoriesAsync();

            if (!Id.HasValue)
            {
                Piece.StartedDate = DateOnly.FromDateTime(DateTime.Today);
                return Page();
            }

            DataHandlerResponse<PieceDetailModel> response = await _pieceHandler.GetByIdAsync(Id.Value);
            if (!response.IsSuccess || response.Data is null)
            {
                return RedirectToPage("Index");
            }

            PieceDetailModel existing = response.Data;
            Piece = new PieceSaveModel
            {
                Title = existing.Title,
                Category = existing.Category,
                Clay = existing.Clay,
                StartedDate = existing.StartedDate,
                FinishedDate = existing.FinishedDate,
                SizeText = existing.SizeText,
                WeightText = existing.WeightText,
                GlazeSummary = existing.GlazeSummary,
                AttachmentsText = existing.AttachmentsText,
                Notes = existing.Notes,
                GlazeApplications = existing.GlazeApplications,
            };
            PieceNumber = existing.PieceNumber;
            Images = existing.Images;

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            Piece.Notes = Piece.Notes.Where(n => !string.IsNullOrWhiteSpace(n.NoteText)).ToList();
            Piece.GlazeApplications = Piece.GlazeApplications.Where(g => !string.IsNullOrWhiteSpace(g.GlazeName)).ToList();

            if (!ModelState.IsValid)
            {
                await LoadExistingCategoriesAsync();
                if (Id.HasValue)
                {
                    DataHandlerResponse<PieceDetailModel> existingResponse = await _pieceHandler.GetByIdAsync(Id.Value);
                    if (existingResponse.IsSuccess && existingResponse.Data is not null)
                    {
                        PieceNumber = existingResponse.Data.PieceNumber;
                        Images = existingResponse.Data.Images;
                    }
                }

                return Page();
            }

            Guid pieceId;
            if (Id.HasValue)
            {
                await _pieceHandler.UpdateAsync(Id.Value, Piece);
                pieceId = Id.Value;
            }
            else
            {
                string createdByEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
                DataHandlerResponse<Guid> createResult = await _pieceHandler.CreateAsync(Piece, createdByEmail);
                pieceId = createResult.Data;
            }

            TempData["StatusMessage"] = "Piece saved.";
            return RedirectToPage("Edit", new { id = pieceId });
        }

        public async Task<IActionResult> OnPostUploadImageAsync(IFormFile? image)
        {
            if (Id.HasValue && image is not null && image.Length > 0)
            {
                await using System.IO.Stream stream = image.OpenReadStream();
                DataHandlerResponse<string> saveResult = await _imageStorageService.SaveResizedJpegAsync(stream, UploadsSubfolders.Pieces);
                if (saveResult.IsSuccess && saveResult.Data is not null)
                {
                    await _pieceHandler.AddImageAsync(Id.Value, saveResult.Data);
                }
            }

            return RedirectToPage("Edit", new { id = Id });
        }

        public async Task<IActionResult> OnPostRemoveImageAsync(Guid imageId)
        {
            DataHandlerResponse<string> response = await _pieceHandler.RemoveImageAsync(imageId);
            if (response.IsSuccess && response.Data is not null)
            {
                _imageStorageService.Delete(UploadsSubfolders.Pieces, response.Data);
            }

            return RedirectToPage("Edit", new { id = Id });
        }

        private async Task LoadExistingCategoriesAsync()
        {
            DataHandlerResponse<List<GalleryCategoryModel>> response = await _pieceHandler.GetGalleryCategoriesAsync();
            if (response.IsSuccess && response.Data is not null)
            {
                ExistingCategories = response.Data.Select(c => c.Category).ToList();
            }
        }
    }
}
