using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PotteryJournal.Web
{
    /// <summary>
    /// Restores the visitor's IP address when the site sits behind Cloudflare's proxy. The forwarded
    /// headers middleware trusts one proxy hop (Render's), so with Cloudflare in front the
    /// connection appears to come from a Cloudflare edge server shared by many visitors.
    /// </summary>
    public static class CloudflareClientIpExtensions
    {
        private const string CONNECTING_IP_HEADER = "CF-Connecting-IP";

        /// <summary>
        /// Adds middleware that replaces <see cref="ConnectionInfo.RemoteIpAddress"/> with the
        /// <c>CF-Connecting-IP</c> header, but only when the connection comes from one of
        /// Cloudflare's published ranges, so a client reaching the origin directly can't spoof it.
        /// Must run before anything that reads the client IP, such as the rate limiter.
        /// </summary>
        /// <param name="app">The application pipeline.</param>
        /// <param name="proxyRanges">Cloudflare's IP ranges in CIDR notation.</param>
        /// <returns>The same pipeline, for chaining.</returns>
        public static IApplicationBuilder UseCloudflareClientIp(this IApplicationBuilder app, IEnumerable<string> proxyRanges)
        {
            List<IPNetwork> networks = proxyRanges.Select(range => IPNetwork.Parse(range)).ToList();
            return app.Use(async (context, next) =>
            {
                context.Connection.RemoteIpAddress = ResolveClientIp(
                    context.Connection.RemoteIpAddress,
                    context.Request.Headers[CONNECTING_IP_HEADER],
                    networks);
                await next(context);
            });
        }

        /// <summary>
        /// Works out the visitor's IP address for one request.
        /// </summary>
        /// <param name="remoteIp">The address the connection appears to come from.</param>
        /// <param name="connectingIpHeader">The <c>CF-Connecting-IP</c> header value, if any.</param>
        /// <param name="proxyRanges">Cloudflare's IP ranges.</param>
        /// <returns>
        /// The header's address when <paramref name="remoteIp"/> is a Cloudflare address and the
        /// header holds a valid IP; otherwise <paramref name="remoteIp"/> unchanged.
        /// </returns>
        public static IPAddress? ResolveClientIp(IPAddress? remoteIp, string? connectingIpHeader, IReadOnlyList<IPNetwork> proxyRanges)
        {
            if (remoteIp is null)
            {
                return remoteIp;
            }

            IPAddress proxyIp = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp;
            bool fromCloudflare = proxyRanges.Any(range => range.Contains(proxyIp));
            if (fromCloudflare && IPAddress.TryParse(connectingIpHeader, out IPAddress? visitorIp))
            {
                return visitorIp;
            }

            return remoteIp;
        }
    }
}
