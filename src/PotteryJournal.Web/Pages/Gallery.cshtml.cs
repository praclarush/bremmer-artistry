using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages
{
    public class GalleryModel : PageModel
    {
        private readonly IPieceHandler _pieceHandler;

        public GalleryModel(IPieceHandler pieceHandler)
        {
            _pieceHandler = pieceHandler;
        }

        public List<GalleryCategoryModel> Categories { get; private set; } = new List<GalleryCategoryModel>();

        public async Task OnGetAsync()
        {
            DataHandlerResponse<List<GalleryCategoryModel>> response = await _pieceHandler.GetGalleryCategoriesAsync();
            if (response.IsSuccess && response.Data is not null)
            {
                Categories = response.Data;
            }
        }
    }
}
