using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Web.Tests
{
    [TestFixture]
    public class ClassesBookingTests
    {
        private PotteryJournalWebApplicationFactory _factory = null!;
        private Guid _classTypeId;
        private DateTimeOffset _slotStart;

        [SetUp]
        public async Task SetUp()
        {
            _factory = new PotteryJournalWebApplicationFactory(Guid.NewGuid().ToString());
            _slotStart = DateTimeOffset.UtcNow.AddDays(10);

            using IServiceScope scope = _factory.Services.CreateScope();
            IReferenceDataHandler referenceDataHandler = scope.ServiceProvider.GetRequiredService<IReferenceDataHandler>();
            // "Wheel Throw"/"Hand-Building" are already auto-seeded by the app's startup hook
            // (EnsureSeedClassTypesAsync), which runs as soon as the factory builds its host -- use
            // a distinct name here to avoid colliding with those.
            DataHandlerResponse<Guid> classTypeResponse = await referenceDataHandler.AddClassTypeAsync("Test Throwing Class", 4);
            Assert.That(classTypeResponse.IsSuccess, Is.True, string.Join(" ", classTypeResponse.Errors));
            _classTypeId = classTypeResponse.Data;

            IClassesHandler classesHandler = scope.ServiceProvider.GetRequiredService<IClassesHandler>();
            await classesHandler.CreateAvailabilityRuleAsync(new ClassAvailabilitySaveModel
            {
                ClassTypeId = _classTypeId,
                StartDateTime = _slotStart,
                RecurrenceFrequency = RecurrenceFrequency.None,
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
        public async Task PostBook_ValidRequest_CreatesTentativeBooking()
        {
            HttpResponseMessage response = await PostBookingAsync();

            Assert.That((int)response.StatusCode, Is.EqualTo(302));

            using IServiceScope scope = _factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            List<ClassBooking> bookings = await context.ClassBookings.ToListAsync();
            Assert.That(bookings, Has.Count.EqualTo(1));
            Assert.That(bookings[0].Status, Is.EqualTo(ClassBookingStatus.Tentative));
        }

        [Test]
        public async Task PostBook_SameSlotTwice_SecondRequestDoesNotCreateASecondBooking()
        {
            await PostBookingAsync();
            await PostBookingAsync();

            using IServiceScope scope = _factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            List<ClassBooking> bookings = await context.ClassBookings.ToListAsync();
            Assert.That(bookings, Has.Count.EqualTo(1));
        }

        private async Task<HttpResponseMessage> PostBookingAsync()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            HttpResponseMessage getResponse = await client.GetAsync("/classes");
            string html = await getResponse.Content.ReadAsStringAsync();
            string antiForgeryToken = ExtractAntiForgeryToken(html);
            string antiForgeryCookie = ExtractAntiForgeryCookie(getResponse);

            using HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, "/classes?handler=Book");
            postRequest.Headers.Add("Cookie", antiForgeryCookie);
            postRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Booking.ClassTypeId"] = _classTypeId.ToString(),
                ["Booking.StartDateTime"] = _slotStart.ToString("O"),
                ["Booking.CustomerName"] = "Jane Doe",
                ["Booking.CustomerEmail"] = "customer@example.com",
                ["Booking.PartySize"] = "2",
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
