using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class DataEndpointsTests
    {
        private PotteryJournalWebApplicationFactory _factory = null!;
        private HttpClient _client = null!;

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
                _client.Dispose();
                _factory.Dispose();
            }
            catch
            {
            }
        }

        [Test]
        public async Task GetPotteryJournalData_NoPieces_ReturnsEmptyJsonArray()
        {
            HttpResponseMessage response = await _client.GetAsync("/pottery-journal/data");

            Assert.That(response.IsSuccessStatusCode, Is.True);
            string body = await response.Content.ReadAsStringAsync();
            Assert.That(body.Trim(), Is.EqualTo("[]"));
        }

        [Test]
        public async Task GetEventsData_NoEvents_ReturnsEmptyJsonArray()
        {
            HttpResponseMessage response = await _client.GetAsync("/events/data");

            Assert.That(response.IsSuccessStatusCode, Is.True);
            string body = await response.Content.ReadAsStringAsync();
            Assert.That(body.Trim(), Is.EqualTo("[]"));
        }

        [Test]
        public async Task GetClassesData_NoAvailability_ReturnsEmptyJsonArray()
        {
            HttpResponseMessage response = await _client.GetAsync("/classes/data");

            Assert.That(response.IsSuccessStatusCode, Is.True);
            string body = await response.Content.ReadAsStringAsync();
            Assert.That(body.Trim(), Is.EqualTo("[]"));
        }

        [Test]
        public async Task GetEventIcs_UnknownId_ReturnsNotFound()
        {
            HttpResponseMessage response = await _client.GetAsync($"/events/{Guid.NewGuid()}/ics");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task GetAdminPieces_Unauthenticated_RedirectsToLogin()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            HttpResponseMessage response = await client.GetAsync("/admin/pieces");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
            Assert.That(response.Headers.Location?.ToString(), Does.Contain("/Admin/Login"));
        }
    }
}
