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
        private const int PageSize = 25;

        private readonly IPieceHandler _pieceHandler;
        private readonly IImageStorageService _imageStorageService;

        public IndexModel(IPieceHandler pieceHandler, IImageStorageService imageStorageService)
        {
            _pieceHandler = pieceHandler;
            _imageStorageService = imageStorageService;
        }

        [BindProperty(SupportsGet = true)]
        public string? Sort { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool Desc { get; set; }

        [BindProperty(SupportsGet = true, Name = "page")]
        public int PageNumber { get; set; } = 1;

        public IReadOnlyList<PieceSummaryModel> Pieces { get; private set; } = new List<PieceSummaryModel>();

        public int TotalPages { get; private set; }

        public int TotalRecords { get; private set; }

        public async Task OnGetAsync()
        {
            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            PagedHandlerResponse<PieceSummaryModel> response = await _pieceHandler.GetSummariesPagedAsync(Sort, Desc, PageNumber, PageSize);
            Pieces = response.Data;
            TotalPages = response.TotalPages;
            TotalRecords = response.TotalRecords;
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
