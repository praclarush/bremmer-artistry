using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace PotteryJournal.Web
{
    /// <summary>
    /// Permanently redirects the site's alternate host names to its one public domain, so search
    /// engines index a single address and visitors can't reach the origin past Cloudflare by name.
    /// </summary>
    public static class CanonicalHostRedirectExtensions
    {
        private const string WWW_PREFIX = "www.";

        /// <summary>
        /// Adds middleware that 301-redirects requests for the platform host name (for example
        /// <c>potteryjournal-web.onrender.com</c>) and the www/non-www twin of
        /// <paramref name="canonicalHost"/> to the same path on <paramref name="canonicalHost"/>.
        /// Other hosts pass through untouched, so health checks on internal addresses still work.
        /// Does nothing when <paramref name="canonicalHost"/> is blank.
        /// </summary>
        /// <param name="app">The application pipeline.</param>
        /// <param name="canonicalHost">The public domain, such as <c>example.com</c>, or blank to disable.</param>
        /// <param name="platformHostname">The hosting platform's own host name for the service, or blank if none.</param>
        /// <returns>The same pipeline, for chaining.</returns>
        public static IApplicationBuilder UseCanonicalHostRedirect(this IApplicationBuilder app, string? canonicalHost, string? platformHostname)
        {
            if (string.IsNullOrWhiteSpace(canonicalHost))
            {
                return app;
            }

            HashSet<string> aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                WwwTwin(canonicalHost),
            };
            if (!string.IsNullOrWhiteSpace(platformHostname))
            {
                aliases.Add(platformHostname);
            }

            HostString canonical = new HostString(canonicalHost);
            return app.Use(async (context, next) =>
            {
                if (aliases.Contains(context.Request.Host.Host))
                {
                    HttpRequest request = context.Request;
                    string target = UriHelper.BuildAbsolute(Uri.UriSchemeHttps, canonical, request.PathBase, request.Path, request.QueryString);
                    context.Response.Redirect(target, true);
                    return;
                }

                await next(context);
            });
        }

        private static string WwwTwin(string host)
        {
            return host.StartsWith(WWW_PREFIX, StringComparison.OrdinalIgnoreCase)
                ? host.Substring(WWW_PREFIX.Length)
                : $"{WWW_PREFIX}{host}";
        }
    }
}
