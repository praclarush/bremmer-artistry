using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PotteryJournal.Infrastructure.Handlers;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class LoginTests
    {
        private PotteryJournalWebApplicationFactory _factory = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString());
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
        public async Task PostAdminLogin_CorrectCredentials_SignsInAndRedirectsToAdmin()
        {
            await AddAdminAsync("owner@example.com", "correct-password");

            HttpResponseMessage postResponse = await PostLoginAsync("owner@example.com", "correct-password");

            Assert.That(postResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
            Assert.That(postResponse.Headers.Location?.ToString(), Does.Contain("/admin").IgnoreCase);
            Assert.That(postResponse.Headers.Contains("Set-Cookie"), Is.True);
        }

        [Test]
        public async Task PostAdminLogin_WrongPassword_ReturnsLoginPageWithoutSettingAuthCookie()
        {
            await AddAdminAsync("owner@example.com", "correct-password");

            HttpResponseMessage postResponse = await PostLoginAsync("owner@example.com", "wrong-password");

            Assert.That(postResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            IEnumerable<string> cookies = postResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values)
                ? values
                : Enumerable.Empty<string>();
            Assert.That(cookies.Any(c => c.StartsWith(".AspNetCore.Cookies", StringComparison.Ordinal)), Is.False);
        }

        private async Task AddAdminAsync(string email, string password)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            IAllowedAdminsHandler allowedAdminsHandler = scope.ServiceProvider.GetRequiredService<IAllowedAdminsHandler>();
            await allowedAdminsHandler.AddAsync(email, password, null, null);
        }

        private async Task<HttpResponseMessage> PostLoginAsync(string email, string password)
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            HttpResponseMessage getResponse = await client.GetAsync("/admin/login");
            string html = await getResponse.Content.ReadAsStringAsync();
            string antiForgeryToken = ExtractAntiForgeryToken(html);
            string antiForgeryCookie = ExtractAntiForgeryCookie(getResponse);

            using HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/login");
            postRequest.Headers.Add("Cookie", antiForgeryCookie);
            postRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = password,
                ["__RequestVerificationToken"] = antiForgeryToken,
            });

            return await client.SendAsync(postRequest);
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
