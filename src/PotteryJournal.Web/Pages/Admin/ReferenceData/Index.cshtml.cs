using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.ReferenceData
{
    public class IndexModel : PageModel
    {
        private readonly IReferenceDataHandler _referenceDataHandler;

        public IndexModel(IReferenceDataHandler referenceDataHandler)
        {
            _referenceDataHandler = referenceDataHandler;
        }

        public List<LookupItemModel> ClayBodies { get; private set; } = new List<LookupItemModel>();

        public List<LookupItemModel> Glazes { get; private set; } = new List<LookupItemModel>();

        public List<LookupItemModel> Categories { get; private set; } = new List<LookupItemModel>();

        public List<CollectionModel> Collections { get; private set; } = new List<CollectionModel>();

        public List<ClassTypeModel> ClassTypes { get; private set; } = new List<ClassTypeModel>();

        [BindProperty]
        public string NewClayBodyName { get; set; } = string.Empty;

        [BindProperty]
        public string NewGlazeName { get; set; } = string.Empty;

        [BindProperty]
        public string NewCategoryName { get; set; } = string.Empty;

        [BindProperty]
        public string NewCollectionName { get; set; } = string.Empty;

        [BindProperty]
        public string NewClassTypeName { get; set; } = string.Empty;

        [BindProperty]
        public int NewClassTypeMaxCapacity { get; set; } = 6;

        public async Task OnGetAsync()
        {
            await LoadAllAsync();
        }

        public async Task<IActionResult> OnPostAddClayBodyAsync()
        {
            DataHandlerResponse<Guid> response = await _referenceDataHandler.AddClayBodyAsync(NewClayBodyName);
            TempData["StatusMessage"] = response.IsSuccess
                ? $"\"{NewClayBodyName.Trim()}\" was added."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddGlazeAsync()
        {
            DataHandlerResponse<Guid> response = await _referenceDataHandler.AddGlazeAsync(NewGlazeName);
            TempData["StatusMessage"] = response.IsSuccess
                ? $"\"{NewGlazeName.Trim()}\" was added."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddCategoryAsync()
        {
            DataHandlerResponse<Guid> response = await _referenceDataHandler.AddCategoryAsync(NewCategoryName);
            TempData["StatusMessage"] = response.IsSuccess
                ? $"\"{NewCategoryName.Trim()}\" was added."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddCollectionAsync()
        {
            DataHandlerResponse<Guid> response = await _referenceDataHandler.AddCollectionAsync(NewCollectionName);
            TempData["StatusMessage"] = response.IsSuccess
                ? $"\"{NewCollectionName.Trim()}\" was added."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddClassTypeAsync()
        {
            DataHandlerResponse<Guid> response = await _referenceDataHandler.AddClassTypeAsync(NewClassTypeName, NewClassTypeMaxCapacity);
            TempData["StatusMessage"] = response.IsSuccess
                ? $"\"{NewClassTypeName.Trim()}\" was added."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateClassTypeCapacityAsync(Guid id, int maxCapacity)
        {
            HandlerResponse response = await _referenceDataHandler.UpdateClassTypeCapacityAsync(id, maxCapacity);
            TempData["StatusMessage"] = response.IsSuccess
                ? "Capacity updated."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveClayBodyAsync(Guid id)
        {
            HandlerResponse response = await _referenceDataHandler.RemoveClayBodyAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "The clay body was removed."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveGlazeAsync(Guid id)
        {
            HandlerResponse response = await _referenceDataHandler.RemoveGlazeAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "The glaze was removed."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveCategoryAsync(Guid id)
        {
            HandlerResponse response = await _referenceDataHandler.RemoveCategoryAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "The category was removed."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveCollectionAsync(Guid id)
        {
            HandlerResponse response = await _referenceDataHandler.RemoveCollectionAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "The collection was removed."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveClassTypeAsync(Guid id)
        {
            HandlerResponse response = await _referenceDataHandler.RemoveClassTypeAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "The class type was removed."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFeatureCollectionAsync(Guid id)
        {
            HandlerResponse response = await _referenceDataHandler.SetCollectionFeaturedAsync(id, true);
            TempData["StatusMessage"] = response.IsSuccess
                ? "That collection is now featured on the homepage."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnfeatureCollectionAsync(Guid id)
        {
            HandlerResponse response = await _referenceDataHandler.SetCollectionFeaturedAsync(id, false);
            TempData["StatusMessage"] = response.IsSuccess
                ? "No collection is featured on the homepage now."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        private async Task LoadAllAsync()
        {
            DataHandlerResponse<List<LookupItemModel>> clayBodiesResponse = await _referenceDataHandler.GetClayBodiesAsync();
            if (clayBodiesResponse.IsSuccess && clayBodiesResponse.Data is not null)
            {
                ClayBodies = clayBodiesResponse.Data;
            }

            DataHandlerResponse<List<LookupItemModel>> glazesResponse = await _referenceDataHandler.GetGlazesAsync();
            if (glazesResponse.IsSuccess && glazesResponse.Data is not null)
            {
                Glazes = glazesResponse.Data;
            }

            DataHandlerResponse<List<LookupItemModel>> categoriesResponse = await _referenceDataHandler.GetCategoriesAsync();
            if (categoriesResponse.IsSuccess && categoriesResponse.Data is not null)
            {
                Categories = categoriesResponse.Data;
            }

            DataHandlerResponse<List<CollectionModel>> collectionsResponse = await _referenceDataHandler.GetCollectionsAsync();
            if (collectionsResponse.IsSuccess && collectionsResponse.Data is not null)
            {
                Collections = collectionsResponse.Data;
            }

            DataHandlerResponse<List<ClassTypeModel>> classTypesResponse = await _referenceDataHandler.GetClassTypesAsync();
            if (classTypesResponse.IsSuccess && classTypesResponse.Data is not null)
            {
                ClassTypes = classTypesResponse.Data;
            }
        }
    }
}
