using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class FaqVisibilityTests
    {
        private const string FAQ_LINK = "href=\"/faq\"";

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
        public async Task GetFaq_NotConfigured_ReturnsNotFound()
        {
            HttpClient client = CreateClient(null);

            HttpResponseMessage response = await client.GetAsync("/faq");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task PublicPage_Hidden_DoesNotLinkToFaq()
        {
            HttpClient client = CreateClient(false);

            string html = await client.GetStringAsync("/");

            Assert.That(html, Does.Not.Contain(FAQ_LINK));
        }

        [Test]
        public async Task GetFaq_Shown_ReturnsOk()
        {
            HttpClient client = CreateClient(true);

            HttpResponseMessage response = await client.GetAsync("/faq");

            Assert.That(response.IsSuccessStatusCode, Is.True);
        }

        [Test]
        public async Task PublicPage_Shown_LinksToFaqInNavAndFooter()
        {
            HttpClient client = CreateClient(true);

            string html = await client.GetStringAsync("/");

            int firstIndex = html.IndexOf(FAQ_LINK, StringComparison.Ordinal);
            int lastIndex = html.LastIndexOf(FAQ_LINK, StringComparison.Ordinal);
            Assert.That(firstIndex, Is.GreaterThan(-1));
            Assert.That(lastIndex, Is.GreaterThan(firstIndex));
        }

        private HttpClient CreateClient(bool? showFaq)
        {
            _factory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString(), true, showFaq);
            _client = _factory.CreateClient();
            return _client;
        }
    }
}
