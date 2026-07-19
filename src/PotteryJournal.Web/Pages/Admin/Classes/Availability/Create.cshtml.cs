using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Classes.Availability
{
    public class CreateModel : PageModel
    {
        private readonly IClassesHandler _classesHandler;
        private readonly IReferenceDataHandler _referenceDataHandler;

        public CreateModel(IClassesHandler classesHandler, IReferenceDataHandler referenceDataHandler)
        {
            _classesHandler = classesHandler;
            _referenceDataHandler = referenceDataHandler;
        }

        [BindProperty]
        public ClassAvailabilitySaveModel Rule { get; set; } = new ClassAvailabilitySaveModel { StartDateTime = DateTimeOffset.Now };

        public List<ClassTypeModel> ClassTypes { get; private set; } = new List<ClassTypeModel>();

        public async Task OnGetAsync()
        {
            await LoadClassTypesAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            DataHandlerResponse<Guid> response = await _classesHandler.CreateAvailabilityRuleAsync(Rule);
            if (!response.IsSuccess)
            {
                await LoadClassTypesAsync();
                foreach (string error in response.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return Page();
            }

            TempData["StatusMessage"] = "Availability rule created.";
            return RedirectToPage("Index");
        }

        private async Task LoadClassTypesAsync()
        {
            DataHandlerResponse<List<ClassTypeModel>> response = await _referenceDataHandler.GetClassTypesAsync();
            if (response.IsSuccess && response.Data is not null)
            {
                ClassTypes = response.Data;
            }
        }
    }
}
