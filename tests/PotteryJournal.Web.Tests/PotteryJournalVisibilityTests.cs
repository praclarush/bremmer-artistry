using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class PotteryJournalVisibilityTests
    {
        private const string JOURNAL_LINK = "href=\"/pottery-journal\"";
        private const string NOINDEX_TAG = "<meta name=\"robots\" content=\"noindex\">";

        private PotteryJournalWebApplicationFactory? _baseFactory;
        private WebApplicationFactory<Program>? _factory;
        private HttpClient? _client;

        [TearDown]
        public void TearDown()
        {
            try
            {
                _client?.Dispose();
                _factory?.Dispose();
                _baseFactory?.Dispose();
            }
            catch
            {
            }
        }

        [TestCase("/")]
        [TestCase("/gallery")]
        [TestCase("/about")]
        public async Task PublicPage_NotConfigured_DoesNotLinkToJournal(string path)
        {
            HttpClient client = CreateClient(null);

            string html = await client.GetStringAsync(path);

            Assert.That(html, Does.Not.Contain(JOURNAL_LINK));
        }

        [TestCase("/")]
        [TestCase("/gallery")]
        public async Task PublicPage_Shown_LinksToJournal(string path)
        {
            HttpClient client = CreateClient(true);

            string html = await client.GetStringAsync(path);

            Assert.That(html, Does.Contain(JOURNAL_LINK));
        }

        [Test]
        public async Task GetJournal_Hidden_StillServesPageWithNoindex()
        {
            HttpClient client = CreateClient(false);

            HttpResponseMessage response = await client.GetAsync("/pottery-journal");
            string html = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(html, Does.Contain(NOINDEX_TAG));
        }

        [Test]
        public async Task GetJournal_Shown_HasNoNoindex()
        {
            HttpClient client = CreateClient(true);

            string html = await client.GetStringAsync("/pottery-journal");

            Assert.That(html, Does.Not.Contain(NOINDEX_TAG));
        }

        [Test]
        public async Task GetSitemap_Shown_ListsJournal()
        {
            HttpClient client = CreateClient(true);

            string sitemap = await client.GetStringAsync("/sitemap.xml");

            Assert.That(sitemap, Does.Contain("/pottery-journal</loc>"));
        }

        private HttpClient CreateClient(bool? showPotteryJournal)
        {
            _baseFactory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString());
            _factory = showPotteryJournal.HasValue
                ? _baseFactory.WithWebHostBuilder(builder => builder.UseSetting("Site:ShowPotteryJournal", showPotteryJournal.Value.ToString()))
                : _baseFactory;
            _client = _factory.CreateClient();
            return _client;
        }
    }
}
