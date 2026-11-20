using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace PotteryJournal.Web
{
    /// <summary>
    /// Maps <c>/robots.txt</c> and <c>/sitemap.xml</c>. Both are generated per request so their
    /// absolute URLs match whichever host serves the site (the Render subdomain or a custom domain).
    /// </summary>
    public static class SearchEngineEndpointExtensions
    {
        private const string ROBOTS_PATH = "/robots.txt";
        private const string SITEMAP_PATH = "/sitemap.xml";
        private const string PLAIN_TEXT_CONTENT_TYPE = "text/plain; charset=utf-8";
        private const string XML_CONTENT_TYPE = "application/xml; charset=utf-8";
        private const string SITEMAP_NAMESPACE = "http://www.sitemaps.org/schemas/sitemap/0.9";

        // Everyone not named in a later group, which includes Googlebot, Bingbot and the other
        // search engines. Robots.txt paths are case-sensitive, and the admin area is reachable in
        // both casings. The /.../data endpoints stay crawlable on purpose: Gallery, Journal and
        // Events render client-side from them, so blocking them would hide those pages' content
        // from Google's renderer.
        private const string ALL_CRAWLERS_GROUP =
            "User-agent: *\n" +
            "Disallow: /admin\n" +
            "Disallow: /Admin\n";

        // Crawlers that collect training data for AI models. None of them affect search ranking;
        // Google-Extended and Applebot-Extended are opt-outs that leave Google and Apple search
        // untouched. AI search fetchers (OAI-SearchBot, ChatGPT-User, Claude-SearchBot,
        // PerplexityBot) are not listed, so the site can still be cited in AI search answers.
        private const string AI_TRAINING_GROUP =
            "User-agent: GPTBot\n" +
            "User-agent: ClaudeBot\n" +
            "User-agent: anthropic-ai\n" +
            "User-agent: CCBot\n" +
            "User-agent: Google-Extended\n" +
            "User-agent: Applebot-Extended\n" +
            "User-agent: Bytespider\n" +
            "User-agent: meta-externalagent\n" +
            "User-agent: Amazonbot\n" +
            "User-agent: cohere-training-data-crawler\n" +
            "User-agent: Diffbot\n" +
            "User-agent: Omgilibot\n" +
            "User-agent: Timpibot\n" +
            "User-agent: ImagesiftBot\n" +
            "Disallow: /\n";

        // SEO and backlink-index crawlers: heavy, frequent, and of no benefit to a small studio site.
        private const string SEO_TOOLS_GROUP =
            "User-agent: AhrefsBot\n" +
            "User-agent: SemrushBot\n" +
            "User-agent: MJ12bot\n" +
            "User-agent: DotBot\n" +
            "User-agent: DataForSeoBot\n" +
            "User-agent: BLEXBot\n" +
            "Disallow: /\n";

        /// <summary>
        /// Maps the <c>/robots.txt</c> and <c>/sitemap.xml</c> endpoints.
        /// </summary>
        /// <param name="app">The application to add the endpoints to.</param>
        /// <returns>The same application, for chaining.</returns>
        public static WebApplication MapSearchEngineEndpoints(this WebApplication app)
        {
            app.MapGet(ROBOTS_PATH, (HttpRequest request) => Results.Text(BuildRobotsTxt(request), PLAIN_TEXT_CONTENT_TYPE));
            app.MapGet(SITEMAP_PATH, (HttpRequest request, IOptions<SiteFeaturesOptions> siteFeatures) =>
                Results.Text(BuildSitemap(request, siteFeatures.Value), XML_CONTENT_TYPE));
            return app;
        }

        private static string BuildRobotsTxt(HttpRequest request)
        {
            StringBuilder robots = new StringBuilder();
            robots.Append(ALL_CRAWLERS_GROUP);
            robots.Append('\n');
            robots.Append(AI_TRAINING_GROUP);
            robots.Append('\n');
            robots.Append(SEO_TOOLS_GROUP);
            robots.Append('\n');
            robots.Append($"Sitemap: {BaseUrl(request)}{SITEMAP_PATH}\n");
            return robots.ToString();
        }

        private static string BuildSitemap(HttpRequest request, SiteFeaturesOptions siteFeatures)
        {
            List<string> paths = new List<string>
            {
                "/",
                "/gallery",
                "/events",
                "/about",
                "/contact",
            };

            if (siteFeatures.ShowPotteryJournal)
            {
                paths.Add("/pottery-journal");
            }

            if (siteFeatures.ShowClasses)
            {
                paths.Add("/classes");
            }

            if (siteFeatures.ShowFaq)
            {
                paths.Add("/faq");
            }

            XNamespace sitemapNamespace = SITEMAP_NAMESPACE;
            string baseUrl = BaseUrl(request);
            XElement urlSet = new XElement(sitemapNamespace + "urlset");
            foreach (string path in paths)
            {
                urlSet.Add(new XElement(sitemapNamespace + "url", new XElement(sitemapNamespace + "loc", $"{baseUrl}{path}")));
            }

            XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", null), urlSet);
            return $"{document.Declaration}\n{document}";
        }

        private static string BaseUrl(HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}";
        }
    }
}
