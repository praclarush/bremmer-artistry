using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class HomeTests
    {
        private const string GalleryTeaserHeading = "From the gallery";

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
        public async Task GetHome_NoContent_RendersWithoutGalleryTeaser()
        {
            HttpResponseMessage response = await _client.GetAsync("/");

            Assert.That(response.IsSuccessStatusCode, Is.True);
            string html = await response.Content.ReadAsStringAsync();
            Assert.That(html, Does.Not.Contain(GalleryTeaserHeading));
            Assert.That(html, Does.Contain("No events scheduled right now"));
        }

        [Test]
        public async Task GetHome_GalleryPieceExists_RendersCategoryTileLinkedToGallery()
        {
            await SeedGalleryPieceAsync("Tea Bowls", "cover.jpg");

            HttpResponseMessage response = await _client.GetAsync("/");

            string html = await response.Content.ReadAsStringAsync();
            Assert.That(html, Does.Contain(GalleryTeaserHeading));
            Assert.That(html, Does.Contain("href=\"/gallery#category/Tea%20Bowls\""));
            Assert.That(html, Does.Contain("/uploads/pieces/cover.jpg"));
            Assert.That(html, Does.Contain("1 piece"));
        }

        [Test]
        public async Task GetHome_UpcomingEvents_RendersNextEventAsFeatureAndRestAsLedgerRows()
        {
            await SeedEventsAsync("Spring Kiln Opening", "Holiday Market");

            HttpResponseMessage response = await _client.GetAsync("/");

            string html = await response.Content.ReadAsStringAsync();
            int featureIndex = html.IndexOf("event-feature-body", StringComparison.Ordinal);
            int ledgerIndex = html.IndexOf("event-ledger-row", StringComparison.Ordinal);
            Assert.That(featureIndex, Is.GreaterThan(-1));
            Assert.That(ledgerIndex, Is.GreaterThan(featureIndex));
            Assert.That(html.IndexOf("Spring Kiln Opening", StringComparison.Ordinal), Is.LessThan(ledgerIndex));
            Assert.That(html.IndexOf("Holiday Market", StringComparison.Ordinal), Is.GreaterThan(ledgerIndex));
        }

        [Test]
        public async Task GetHome_UpcomingEvent_LinksToItsOccurrenceCardOnEventsPage()
        {
            // events.js gives each card the id "event-<id>-<start as Unix milliseconds>", so the
            // Home link has to carry the same occurrence key for the Events page to find the card.
            Event upcoming = BuildEvent("Spring Kiln Opening", DateTimeOffset.UtcNow.AddDays(7));
            using (IServiceScope scope = _factory.Services.CreateScope())
            {
                AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Events.Add(upcoming);
                await context.SaveChangesAsync();
            }

            HttpResponseMessage response = await _client.GetAsync("/");

            string html = await response.Content.ReadAsStringAsync();
            string expectedHref = $"href=\"/events#event-{upcoming.Id}-{upcoming.StartDateTime.ToUnixTimeMilliseconds()}\"";
            Assert.That(html, Does.Contain(expectedHref));
        }

        private async Task SeedGalleryPieceAsync(string categoryName, string imageFileName)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Category category = new Category { Id = Guid.NewGuid(), Name = categoryName };
            context.Categories.Add(category);
            context.Pieces.Add(new Piece
            {
                Id = Guid.NewGuid(),
                PieceNumber = 1,
                Title = "Piece 1",
                CategoryId = category.Id,
                ShowInGallery = true,
                StartedDate = new DateOnly(2026, 1, 1),
                CreatedByEmail = "admin@example.com",
                Images = new List<PieceImage>
                {
                    new PieceImage { Id = Guid.NewGuid(), FileName = imageFileName, SortOrder = 0 },
                },
            });
            await context.SaveChangesAsync();
        }

        private async Task SeedEventsAsync(string firstTitle, string secondTitle)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            context.Events.Add(BuildEvent(firstTitle, now.AddDays(7)));
            context.Events.Add(BuildEvent(secondTitle, now.AddDays(30)));
            await context.SaveChangesAsync();
        }

        private static Event BuildEvent(string title, DateTimeOffset start)
        {
            return new Event
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = "Details",
                StartDateTime = start,
                VenueName = "Studio",
                CreatedByEmail = "admin@example.com",
            };
        }
    }
}
