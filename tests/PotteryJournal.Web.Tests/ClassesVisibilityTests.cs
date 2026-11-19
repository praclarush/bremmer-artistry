using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class ClassesVisibilityTests
    {
        private const string CLASSES_NAV_LINK = "href=\"/classes\"";
        private const string ANTIFORGERY_FIELD_NAME = "__RequestVerificationToken";
        private const string ANTIFORGERY_TOKEN_PATTERN = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"";

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
        public async Task GetClasses_Hidden_ReturnsNotFound()
        {
            HttpClient client = CreateClient(showClasses: false);

            HttpResponseMessage response = await client.GetAsync("/classes");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task GetClassesData_Hidden_ReturnsNotFound()
        {
            HttpClient client = CreateClient(showClasses: false);

            HttpResponseMessage response = await client.GetAsync("/classes/data");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task PostBook_Hidden_ReturnsNotFound()
        {
            HttpClient client = CreateClient(showClasses: false);
            // Without a valid token, antiforgery rejects the POST (400) before the handler's own
            // gate runs. Antiforgery tokens aren't page-specific, so borrow one from Contact.
            string contactHtml = await client.GetStringAsync("/contact");
            Match tokenMatch = Regex.Match(contactHtml, ANTIFORGERY_TOKEN_PATTERN);
            Dictionary<string, string> form = new Dictionary<string, string>
            {
                [ANTIFORGERY_FIELD_NAME] = tokenMatch.Groups[1].Value,
            };

            HttpResponseMessage response = await client.PostAsync("/classes?handler=Book", new FormUrlEncodedContent(form));

            Assert.That(tokenMatch.Success, Is.True);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [TestCase("/")]
        [TestCase("/gallery")]
        [TestCase("/contact")]
        public async Task PublicPage_Hidden_DoesNotLinkToClasses(string path)
        {
            HttpClient client = CreateClient(showClasses: false);

            string html = await client.GetStringAsync(path);

            Assert.That(html, Does.Not.Contain(CLASSES_NAV_LINK));
        }

        [Test]
        public async Task Contact_Hidden_DoesNotMentionClasses()
        {
            HttpClient client = CreateClient(showClasses: false);

            string html = await client.GetStringAsync("/contact");

            Assert.That(html, Does.Not.Contain("a piece, a class"));
            Assert.That(html, Does.Contain("a piece or an event"));
        }

        [Test]
        public async Task PublicPage_Shown_LinksToClassesInNavAndFooter()
        {
            HttpClient client = CreateClient(showClasses: true);

            string html = await client.GetStringAsync("/");

            int firstIndex = html.IndexOf(CLASSES_NAV_LINK, StringComparison.Ordinal);
            int lastIndex = html.LastIndexOf(CLASSES_NAV_LINK, StringComparison.Ordinal);
            Assert.That(firstIndex, Is.GreaterThan(-1));
            Assert.That(lastIndex, Is.GreaterThan(firstIndex));
        }

        [Test]
        public async Task GetClasses_Shown_ReturnsOk()
        {
            HttpClient client = CreateClient(showClasses: true);

            HttpResponseMessage response = await client.GetAsync("/classes");

            Assert.That(response.IsSuccessStatusCode, Is.True);
        }

        [Test]
        public async Task GetClasses_NotConfigured_ReturnsNotFound()
        {
            // Production default: nothing in config means hidden.
            _factory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString(), showClasses: null);
            _client = _factory.CreateClient();

            HttpResponseMessage response = await _client.GetAsync("/classes");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        private HttpClient CreateClient(bool showClasses)
        {
            _factory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString(), showClasses);
            _client = _factory.CreateClient();
            return _client;
        }
    }
}
