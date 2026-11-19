using System.Collections.Generic;
using System.Linq;
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

        public List<string> ErrorMessages { get; private set; } = new List<string>();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            HandlerResponse response = await _contactHandler.SubmitAsync(Name, Email, Message);
            if (!response.IsSuccess)
            {
                // Re-render rather than redirect so the visitor's name, email, and message stay
                // in the form -- a redirect would throw away a carefully written inquiry.
                ErrorMessages = response.Errors.ToList();
                return Page();
            }

            TempData["StatusMessage"] = "Thanks! Your message has been sent. We'll reply by email.";
            return RedirectToPage();
        }
    }
}
