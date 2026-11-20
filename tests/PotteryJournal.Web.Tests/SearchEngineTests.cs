using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class SearchEngineTests
    {
        private PotteryJournalWebApplicationFactory? _factory;
        private HttpClient? _client;

        [TearDown]
        public void TearDown()
        {
            try
            {
                _client?.Dispose();
                _factory?.Dispose();
            }
            catch
            {
            }
        }

        [Test]
        public async Task GetRobotsTxt_ReturnsPlainTextWithAbsoluteSitemap()
        {
            HttpClient client = CreateClient(null, null);

            HttpResponseMessage response = await client.GetAsync("/robots.txt");
            string robots = await response.Content.ReadAsStringAsync();

            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/plain"));
            Assert.That(robots, Does.Contain($"Sitemap: {client.BaseAddress?.GetLeftPart(UriPartial.Authority)}/sitemap.xml"));
        }

        [Test]
        public async Task GetRobotsTxt_AllowsSearchEnginesExceptAdmin()
        {
            HttpClient client = CreateClient(null, null);

            string robots = await client.GetStringAsync("/robots.txt");

            Assert.That(robots, Does.Contain("User-agent: *\nDisallow: /admin\nDisallow: /Admin\n"));
            Assert.That(robots, Does.Not.Contain("User-agent: Googlebot"));
            Assert.That(robots, Does.Not.Contain("Disallow: /gallery"));
        }

        [Test]
        public async Task GetRobotsTxt_BlocksAiTrainingAndSeoCrawlers()
        {
            HttpClient client = CreateClient(null, null);

            string robots = await client.GetStringAsync("/robots.txt");

            Assert.That(robots, Does.Contain("User-agent: GPTBot\n"));
            Assert.That(robots, Does.Contain("User-agent: CCBot\n"));
            Assert.That(robots, Does.Contain("User-agent: AhrefsBot\n"));
            Assert.That(robots, Does.Contain("Disallow: /\n"));
        }

        [Test]
        public async Task GetSitemap_SectionsHidden_ListsPublicPagesOnly()
        {
            HttpClient client = CreateClient(false, false);

            string sitemap = await client.GetStringAsync("/sitemap.xml");

            Assert.That(sitemap, Does.Contain("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">"));
            Assert.That(sitemap, Does.Contain("/gallery</loc>"));
            Assert.That(sitemap, Does.Not.Contain("/pottery-journal</loc>"));
            Assert.That(sitemap, Does.Not.Contain("/classes</loc>"));
            Assert.That(sitemap, Does.Not.Contain("/faq</loc>"));
            Assert.That(sitemap, Does.Not.Contain("/admin"));
        }

        [Test]
        public async Task GetSitemap_SectionsShown_ListsClassesAndFaq()
        {
            HttpClient client = CreateClient(true, true);

            string sitemap = await client.GetStringAsync("/sitemap.xml");

            Assert.That(sitemap, Does.Contain("/classes</loc>"));
            Assert.That(sitemap, Does.Contain("/faq</loc>"));
        }

        private HttpClient CreateClient(bool? showClasses, bool? showFaq)
        {
            _factory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString(), showClasses, showFaq);
            _client = _factory.CreateClient();
            return _client;
        }
    }
}
