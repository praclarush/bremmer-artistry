using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PotteryJournal.Infrastructure.Data;

namespace PotteryJournal.Web.Tests
{
    /// <summary>
    /// A <see cref="WebApplicationFactory{TEntryPoint}"/> that swaps the Postgres-backed
    /// <see cref="AppDbContext"/> for an isolated EF Core in-memory database, so integration tests
    /// don't require a real Postgres instance.
    /// </summary>
    public class PotteryJournalWebApplicationFactory : WebApplicationFactory<Program>
    {
        private const string SHOW_CLASSES_KEY = "Site:ShowClasses";
        private const string SHOW_FAQ_KEY = "Site:ShowFaq";

        private readonly string _databaseName;
        private readonly bool? _showClasses;
        private readonly bool? _showFaq;

        /// <summary>
        /// Initializes a new instance of <see cref="PotteryJournalWebApplicationFactory"/> with the
        /// public Classes and FAQ sections shown, since most tests exercise them.
        /// </summary>
        /// <param name="databaseName">A unique name for this instance's in-memory database.</param>
        public PotteryJournalWebApplicationFactory(string databaseName)
            : this(databaseName, true, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PotteryJournalWebApplicationFactory"/> with an
        /// explicit setting for the public Classes section and the FAQ left unset (hidden).
        /// </summary>
        /// <param name="databaseName">A unique name for this instance's in-memory database.</param>
        /// <param name="showClasses">The <c>Site:ShowClasses</c> value to configure, or null to leave it unset (the production default, hidden).</param>
        public PotteryJournalWebApplicationFactory(string databaseName, bool? showClasses)
            : this(databaseName, showClasses, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PotteryJournalWebApplicationFactory"/> with
        /// explicit settings for the public Classes and FAQ sections.
        /// </summary>
        /// <param name="databaseName">A unique name for this instance's in-memory database.</param>
        /// <param name="showClasses">The <c>Site:ShowClasses</c> value to configure, or null to leave it unset (hidden).</param>
        /// <param name="showFaq">The <c>Site:ShowFaq</c> value to configure, or null to leave it unset (hidden).</param>
        public PotteryJournalWebApplicationFactory(string databaseName, bool? showClasses, bool? showFaq)
        {
            _databaseName = databaseName;
            _showClasses = showClasses;
            _showFaq = showFaq;
        }

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Dictionary<string, string?> siteSettings = new Dictionary<string, string?>();
            if (_showClasses.HasValue)
            {
                siteSettings[SHOW_CLASSES_KEY] = _showClasses.Value.ToString();
            }

            if (_showFaq.HasValue)
            {
                siteSettings[SHOW_FAQ_KEY] = _showFaq.Value.ToString();
            }

            if (siteSettings.Count > 0)
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(siteSettings);
                });
            }

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
