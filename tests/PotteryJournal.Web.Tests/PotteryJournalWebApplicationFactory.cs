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
        private readonly string _databaseName;

        /// <summary>
        /// Initializes a new instance of <see cref="PotteryJournalWebApplicationFactory"/>.
        /// </summary>
        /// <param name="databaseName">A unique name for this instance's in-memory database.</param>
        public PotteryJournalWebApplicationFactory(string databaseName)
        {
            _databaseName = databaseName;
        }

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // ASP.NET Core's remote authentication handlers (Google) validate their options on
            // every request, not just authenticated ones, to check whether the request matches
            // their OAuth callback path. Without a non-empty ClientId/ClientSecret, even anonymous
            // requests throw -- so tests need placeholder values, though the OAuth flow itself is
            // never exercised here.
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Google:ClientId"] = "test-client-id",
                    ["Authentication:Google:ClientSecret"] = "test-client-secret",
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
