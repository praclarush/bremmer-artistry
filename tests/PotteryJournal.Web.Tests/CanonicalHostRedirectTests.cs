using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class CanonicalHostRedirectTests
    {
        private const string CANONICAL_HOST = "bremmerartistry.com";
        private const string PLATFORM_HOST = "potteryjournal-web.onrender.com";

        private PotteryJournalWebApplicationFactory? _baseFactory;
        private WebApplicationFactory<Program>? _factory;

        [SetUp]
        public void SetUp()
        {
            _baseFactory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString());
            _factory = _baseFactory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Site:CanonicalHost", CANONICAL_HOST);
                builder.UseSetting("RENDER_EXTERNAL_HOSTNAME", PLATFORM_HOST);
            });
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _factory?.Dispose();
                _baseFactory?.Dispose();
            }
            catch
            {
            }
        }

        [TestCase(PLATFORM_HOST)]
        [TestCase("www." + CANONICAL_HOST)]
        public async Task Get_AliasHost_RedirectsPermanentlyToCanonicalHost(string host)
        {
            HttpResponseMessage response = await GetAsync(host, "/gallery?category=Mugs");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MovedPermanently));
            Assert.That(response.Headers.Location, Is.EqualTo(new Uri($"https://{CANONICAL_HOST}/gallery?category=Mugs")));
        }

        [TestCase(CANONICAL_HOST)]
        [TestCase("10.0.0.5")]
        public async Task Get_CanonicalOrInternalHost_IsNotRedirected(string host)
        {
            HttpResponseMessage response = await GetAsync(host, "/about");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        private async Task<HttpResponseMessage> GetAsync(string host, string pathAndQuery)
        {
            using HttpClient client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri($"https://{host}"),
            });
            return await client.GetAsync(pathAndQuery);
        }
    }
}
