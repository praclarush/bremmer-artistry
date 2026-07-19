using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Pages
{
    [EnableRateLimiting(RateLimiterPolicies.ContactForm)]
    public class ContactModel : PageModel
    {
        private readonly IContactHandler _contactHandler;

        public ContactModel(IContactHandler contactHandler)
        {
            _contactHandler = contactHandler;
        }

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Message { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            HandlerResponse response = await _contactHandler.SubmitAsync(Name, Email, Message);
            TempData["StatusMessage"] = response.IsSuccess
                ? "Thanks! Your message has been sent."
                : string.Join(" ", response.Errors);
            return RedirectToPage();
        }
    }
}
