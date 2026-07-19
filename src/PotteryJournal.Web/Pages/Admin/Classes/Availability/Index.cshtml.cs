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
    public class IndexModel : PageModel
    {
        private readonly IClassesHandler _classesHandler;

        public IndexModel(IClassesHandler classesHandler)
        {
            _classesHandler = classesHandler;
        }

        public List<ClassAvailabilityModel> Rules { get; private set; } = new List<ClassAvailabilityModel>();

        public async Task OnGetAsync()
        {
            DataHandlerResponse<List<ClassAvailabilityModel>> response = await _classesHandler.GetAvailabilityRulesAsync();
            if (response.IsSuccess && response.Data is not null)
            {
                Rules = response.Data;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            HandlerResponse response = await _classesHandler.DeleteAvailabilityRuleAsync(id);
            TempData["StatusMessage"] = response.IsSuccess
                ? "The availability rule was deleted."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }

        public static string FormatRecurrence(ClassAvailabilityModel rule)
        {
            if (rule.RecurrenceFrequency == RecurrenceFrequency.None)
            {
                return "Does not repeat";
            }

            string unit = rule.RecurrenceFrequency.ToString().ToLowerInvariant();
            string description = rule.RecurrenceInterval > 1
                ? $"Every {rule.RecurrenceInterval} {unit}s"
                : $"Every {unit}";

            if (rule.RecurrenceEndDate.HasValue)
            {
                description += $" until {rule.RecurrenceEndDate.Value:yyyy-MM-dd}";
            }

            return description;
        }
    }
}
