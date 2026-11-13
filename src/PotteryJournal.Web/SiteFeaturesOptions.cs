namespace PotteryJournal.Web
{
    /// <summary>
    /// Switches for optional public-site sections, bound from the "Site" configuration section.
    /// </summary>
    public class SiteFeaturesOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the public Classes section is available: its
        /// nav and footer links, the /classes page and its booking form, and the /classes/data
        /// endpoint. Off by default. The admin area for classes is unaffected.
        /// </summary>
        public bool ShowClasses { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the public FAQ page is available: its nav and
        /// footer links and the /faq page itself. Off by default until real FAQ copy is written.
        /// </summary>
        public bool ShowFaq { get; set; }
    }
}
