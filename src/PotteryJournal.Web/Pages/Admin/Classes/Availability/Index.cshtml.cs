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

        private static readonly (ClassAvailabilityDays Day, string Abbreviation)[] _dayOrder =
        {
            (ClassAvailabilityDays.Sunday, "Sun"),
            (ClassAvailabilityDays.Monday, "Mon"),
            (ClassAvailabilityDays.Tuesday, "Tue"),
            (ClassAvailabilityDays.Wednesday, "Wed"),
            (ClassAvailabilityDays.Thursday, "Thu"),
            (ClassAvailabilityDays.Friday, "Fri"),
            (ClassAvailabilityDays.Saturday, "Sat"),
        };

        public static string FormatDaysOfWeek(ClassAvailabilityModel rule)
        {
            List<string> days = new List<string>();
            foreach ((ClassAvailabilityDays day, string abbreviation) in _dayOrder)
            {
                if (rule.DaysOfWeek.HasFlag(day))
                {
                    days.Add(abbreviation);
                }
            }

            return days.Count > 0 ? string.Join(", ", days) : "None";
        }

        public static string FormatTimes(ClassAvailabilityModel rule)
        {
            string start = DateTime.Today.Add(rule.StartTime).ToString("h:mm tt");
            if (rule.LastStartTime <= rule.StartTime)
            {
                return start;
            }

            string last = DateTime.Today.Add(rule.LastStartTime).ToString("h:mm tt");
            return $"{start} – {last}, every {rule.IntervalHours}h";
        }
    }
}
