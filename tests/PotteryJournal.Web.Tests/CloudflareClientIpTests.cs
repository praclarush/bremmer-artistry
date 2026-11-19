using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class CloudflareClientIpTests
    {
        private const string CONNECTING_IP_HEADER = "CF-Connecting-IP";
        private const string CLOUDFLARE_EDGE_IP = "173.245.48.5";
        private const string VISITOR_IP = "203.0.113.10";
        private const string OTHER_VISITOR_IP = "198.51.100.20";
        private const string LOGIN_PATH = "/admin/login";

        // Mirrors the LoginAttempts policy's PermitLimit in Program.cs.
        private const int LOGIN_ATTEMPTS_PER_CLIENT_PER_MINUTE = 10;

        private static readonly IReadOnlyList<IPNetwork> CLOUDFLARE_RANGES = new List<IPNetwork>
        {
            IPNetwork.Parse("173.245.48.0/20"),
            IPNetwork.Parse("2400:cb00::/32"),
        };

        private PotteryJournalWebApplicationFactory? _factory;

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
                _factory?.Dispose();
            }
            catch
            {
            }
        }

        [TestCase(CLOUDFLARE_EDGE_IP, VISITOR_IP, VISITOR_IP)]
        [TestCase("::ffff:173.245.48.5", VISITOR_IP, VISITOR_IP)]
        [TestCase("2400:cb00::1", "2001:db8::7", "2001:db8::7")]
        [TestCase(OTHER_VISITOR_IP, VISITOR_IP, OTHER_VISITOR_IP)]
        [TestCase(CLOUDFLARE_EDGE_IP, "not-an-ip", CLOUDFLARE_EDGE_IP)]
        [TestCase(CLOUDFLARE_EDGE_IP, null, CLOUDFLARE_EDGE_IP)]
        public void ResolveClientIp_ReturnsHeaderOnlyFromCloudflare(string remoteIp, string? header, string expected)
        {
            IPAddress? result = CloudflareClientIpExtensions.ResolveClientIp(IPAddress.Parse(remoteIp), header, CLOUDFLARE_RANGES);

            Assert.That(result, Is.EqualTo(IPAddress.Parse(expected)));
        }

        [Test]
        public void ResolveClientIp_NoRemoteIp_ReturnsNull()
        {
            IPAddress? result = CloudflareClientIpExtensions.ResolveClientIp(null, VISITOR_IP, CLOUDFLARE_RANGES);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task Login_VisitorsBehindSameCloudflareEdge_GetSeparateLimits()
        {
            for (int i = 0; i < LOGIN_ATTEMPTS_PER_CLIENT_PER_MINUTE; i++)
            {
                int allowedStatus = await GetLoginStatusViaCloudflareAsync(VISITOR_IP);
                Assert.That(allowedStatus, Is.EqualTo(StatusCodes.Status200OK), $"Request {i + 1} was rejected early.");
            }

            int rejectedStatus = await GetLoginStatusViaCloudflareAsync(VISITOR_IP);
            int otherVisitorStatus = await GetLoginStatusViaCloudflareAsync(OTHER_VISITOR_IP);

            Assert.That(rejectedStatus, Is.EqualTo(StatusCodes.Status429TooManyRequests));
            Assert.That(otherVisitorStatus, Is.EqualTo(StatusCodes.Status200OK));
        }

        private async Task<int> GetLoginStatusViaCloudflareAsync(string visitorIp)
        {
            HttpContext context = await _factory!.Server.SendAsync(request =>
            {
                request.Request.Method = HttpMethods.Get;
                request.Request.Path = LOGIN_PATH;
                request.Request.Headers[CONNECTING_IP_HEADER] = visitorIp;
                request.Connection.RemoteIpAddress = IPAddress.Parse(CLOUDFLARE_EDGE_IP);
            });
            return context.Response.StatusCode;
        }
    }
}
