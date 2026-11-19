using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace PotteryJournal.Web.Pages
{
    public class FaqModel : PageModel
    {
        private readonly SiteFeaturesOptions _siteFeatures;

        public FaqModel(IOptions<SiteFeaturesOptions> siteFeatures)
        {
            _siteFeatures = siteFeatures.Value;
        }

        public IActionResult OnGet()
        {
            return _siteFeatures.ShowFaq ? Page() : NotFound();
        }
    }
}
