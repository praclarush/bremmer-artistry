using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages.Admin.Classes.Availability
{
    public class EditModel : PageModel
    {
        private readonly IClassesHandler _classesHandler;
        private readonly IReferenceDataHandler _referenceDataHandler;

        public EditModel(IClassesHandler classesHandler, IReferenceDataHandler referenceDataHandler)
        {
            _classesHandler = classesHandler;
            _referenceDataHandler = referenceDataHandler;
        }

        [BindProperty(SupportsGet = true)]
        public Guid? Id { get; set; }

        [BindProperty]
        public ClassAvailabilitySaveModel Rule { get; set; } = new ClassAvailabilitySaveModel();

        public List<ClassTypeModel> ClassTypes { get; private set; } = new List<ClassTypeModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadClassTypesAsync();

            if (!Id.HasValue)
            {
                return Page();
            }

            DataHandlerResponse<ClassAvailabilityModel> response = await _classesHandler.GetAvailabilityRuleByIdAsync(Id.Value);
            if (!response.IsSuccess || response.Data is null)
            {
                return RedirectToPage("Index");
            }

            ClassAvailabilityModel existing = response.Data;
            Rule = new ClassAvailabilitySaveModel
            {
                ClassTypeId = existing.ClassTypeId,
                DaysOfWeek = SplitDaysOfWeek(existing.DaysOfWeek),
                StartTime = existing.StartTime,
                RepeatsMultipleTimesPerDay = existing.LastStartTime > existing.StartTime,
                LastStartTime = existing.LastStartTime,
                IntervalHours = existing.IntervalHours,
            };

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (Id.HasValue)
            {
                HandlerResponse updateResponse = await _classesHandler.UpdateAvailabilityRuleAsync(Id.Value, Rule);
                if (!updateResponse.IsSuccess)
                {
                    await LoadClassTypesAsync();
                    foreach (string error in updateResponse.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }

                    return Page();
                }

                TempData["StatusMessage"] = "Availability rule updated.";
                return RedirectToPage("Index");
            }

            DataHandlerResponse<Guid> createResponse = await _classesHandler.CreateAvailabilityRuleAsync(Rule);
            if (!createResponse.IsSuccess)
            {
                await LoadClassTypesAsync();
                foreach (string error in createResponse.Errors)
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

        private static List<ClassAvailabilityDays> SplitDaysOfWeek(ClassAvailabilityDays daysOfWeek)
        {
            List<ClassAvailabilityDays> days = new List<ClassAvailabilityDays>();
            foreach (ClassAvailabilityDays day in Enum.GetValues<ClassAvailabilityDays>())
            {
                if (day != ClassAvailabilityDays.None && daysOfWeek.HasFlag(day))
                {
                    days.Add(day);
                }
            }

            return days;
        }
    }
}
