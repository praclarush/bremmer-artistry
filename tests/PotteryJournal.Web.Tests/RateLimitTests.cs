using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class RateLimitTests
    {
        // Mirrors requestsPerClientPerMinute in Program.cs.
        private const int REQUESTS_PER_CLIENT_PER_MINUTE = 600;

        // Mirrors the LoginAttempts policy's PermitLimit in Program.cs.
        private const int LOGIN_ATTEMPTS_PER_CLIENT_PER_MINUTE = 10;

        private const string LOGIN_PATH = "/admin/login";

        private PotteryJournalWebApplicationFactory? _factory;
        private HttpClient? _client;

        [SetUp]
        public void SetUp()
        {
            _factory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString());
            _client = _factory.CreateClient();
        }

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
        public async Task StaticFile_OverPerClientLimit_ReturnsTooManyRequests()
        {
            for (int i = 0; i < REQUESTS_PER_CLIENT_PER_MINUTE; i++)
            {
                HttpResponseMessage allowed = await _client!.GetAsync("/favicon.svg");
                Assert.That(allowed.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Request {i + 1} was rejected early.");
            }

            HttpResponseMessage rejected = await _client!.GetAsync("/favicon.svg");

            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }

        [Test]
        public async Task Login_OneClientOverLimit_OtherClientStillAllowed()
        {
            IPAddress guessingClient = IPAddress.Parse("203.0.113.10");
            IPAddress realAdmin = IPAddress.Parse("198.51.100.20");
            for (int i = 0; i < LOGIN_ATTEMPTS_PER_CLIENT_PER_MINUTE; i++)
            {
                int allowedStatus = await GetLoginStatusAsync(guessingClient);
                Assert.That(allowedStatus, Is.EqualTo(StatusCodes.Status200OK), $"Request {i + 1} was rejected early.");
            }

            int rejectedStatus = await GetLoginStatusAsync(guessingClient);
            int otherClientStatus = await GetLoginStatusAsync(realAdmin);

            Assert.That(rejectedStatus, Is.EqualTo(StatusCodes.Status429TooManyRequests));
            Assert.That(otherClientStatus, Is.EqualTo(StatusCodes.Status200OK));
        }

        private async Task<int> GetLoginStatusAsync(IPAddress clientAddress)
        {
            HttpContext context = await _factory!.Server.SendAsync(request =>
            {
                request.Request.Method = HttpMethods.Get;
                request.Request.Path = LOGIN_PATH;
                request.Connection.RemoteIpAddress = clientAddress;
            });
            return context.Response.StatusCode;
        }
    }
}
