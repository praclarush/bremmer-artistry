using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Constants;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Pieces
{
    public class IndexModel : PageModel
    {
        private readonly IPieceHandler _pieceHandler;
        private readonly IImageStorageService _imageStorageService;

        public IndexModel(IPieceHandler pieceHandler, IImageStorageService imageStorageService)
        {
            _pieceHandler = pieceHandler;
            _imageStorageService = imageStorageService;
        }

        public List<PieceSummaryModel> Pieces { get; private set; } = new List<PieceSummaryModel>();

        public async Task OnGetAsync()
        {
            DataHandlerResponse<List<PieceSummaryModel>> response = await _pieceHandler.GetSummariesAsync(null);
            if (response.IsSuccess && response.Data is not null)
            {
                Pieces = response.Data;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            DataHandlerResponse<PieceDetailModel> existing = await _pieceHandler.GetByIdAsync(id);
            if (existing.IsSuccess && existing.Data is not null)
            {
                foreach (PieceImageModel image in existing.Data.Images)
                {
                    _imageStorageService.Delete(UploadsSubfolders.Pieces, image.FileName);
                }
            }

            HandlerResponse response = await _pieceHandler.DeleteAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "The piece was deleted."
                : string.Join(" ", response.Errors);

            return RedirectToPage();
        }
    }
}
