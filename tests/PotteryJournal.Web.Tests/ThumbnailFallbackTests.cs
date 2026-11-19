using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class ThumbnailFallbackTests
    {
        private const string FULL_CONTENT = "full-size photo";
        private const string THUMBNAIL_CONTENT = "thumbnail";
        private const string THUMBNAIL_URL = "/uploads/pieces/thumbs/photo.jpg";

        private string _uploadsRoot = string.Empty;
        private PotteryJournalWebApplicationFactory? _factory;
        private WebApplicationFactory<Program>? _configuredFactory;
        private HttpClient? _client;

        [SetUp]
        public void SetUp()
        {
            _uploadsRoot = Path.Combine(Path.GetTempPath(), $"pj-web-uploads-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(_uploadsRoot, "pieces", "thumbs"));
            File.WriteAllText(Path.Combine(_uploadsRoot, "pieces", "photo.jpg"), FULL_CONTENT);

            _factory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString());
            _configuredFactory = _factory.WithWebHostBuilder(builder => builder.UseSetting("Uploads:RootPath", _uploadsRoot));
            _client = _configuredFactory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _client?.Dispose();
                _configuredFactory?.Dispose();
                _factory?.Dispose();
                Directory.Delete(_uploadsRoot, true);
            }
            catch
            {
            }
        }

        [Test]
        public async Task GetThumbnail_Missing_ServesFullPhotoUncached()
        {
            HttpResponseMessage response = await _client!.GetAsync(THUMBNAIL_URL);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(FULL_CONTENT));
            Assert.That(response.Headers.CacheControl?.NoCache, Is.True);
        }

        [Test]
        public async Task GetThumbnail_Exists_ServesThumbnailWithLongCache()
        {
            File.WriteAllText(Path.Combine(_uploadsRoot, "pieces", "thumbs", "photo.jpg"), THUMBNAIL_CONTENT);

            HttpResponseMessage response = await _client!.GetAsync(THUMBNAIL_URL);

            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(THUMBNAIL_CONTENT));
            Assert.That(response.Headers.CacheControl?.MaxAge, Is.EqualTo(TimeSpan.FromDays(365)));
        }

        [Test]
        public async Task GetThumbnail_NeitherExists_ReturnsNotFound()
        {
            HttpResponseMessage response = await _client!.GetAsync("/uploads/pieces/thumbs/missing.jpg");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}
