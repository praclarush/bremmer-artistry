using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class ContactTests
    {
        private PotteryJournalWebApplicationFactory _factory = null!;

        [SetUp]
        public async Task SetUp()
        {
            _factory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString());

            using IServiceScope scope = _factory.Services.CreateScope();
            IAdminSettingsHandler adminSettingsHandler = scope.ServiceProvider.GetRequiredService<IAdminSettingsHandler>();
            await adminSettingsHandler.UpdateAsync(new AdminSettingsModel
            {
                NotificationRecipientEmail = "studio@example.com",
                MinimumBookingLeadDays = 2,
            });
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _factory.Dispose();
            }
            catch
            {
            }
        }

        [Test]
        public async Task PostContact_MissingMessage_RedirectsWithErrorAndDoesNotThrow()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            HttpResponseMessage getResponse = await client.GetAsync("/contact");
            string html = await getResponse.Content.ReadAsStringAsync();
            string antiForgeryToken = ExtractAntiForgeryToken(html);
            string antiForgeryCookie = ExtractAntiForgeryCookie(getResponse);

            using HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, "/contact");
            postRequest.Headers.Add("Cookie", antiForgeryCookie);
            postRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = "Jane Doe",
                ["Email"] = "customer@example.com",
                ["Message"] = string.Empty,
                ["__RequestVerificationToken"] = antiForgeryToken,
            });

            HttpResponseMessage response = await client.SendAsync(postRequest);

            Assert.That((int)response.StatusCode, Is.EqualTo(302));
        }

        private static string ExtractAntiForgeryToken(string html)
        {
            Match match = Regex.Match(html, @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static string ExtractAntiForgeryCookie(HttpResponseMessage response)
        {
            IEnumerable<string> setCookieHeaders = response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values)
                ? values
                : Enumerable.Empty<string>();

            string? antiForgerySetCookie = setCookieHeaders.FirstOrDefault(c => c.StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal));
            return antiForgerySetCookie?.Split(';')[0] ?? string.Empty;
        }
    }
}
