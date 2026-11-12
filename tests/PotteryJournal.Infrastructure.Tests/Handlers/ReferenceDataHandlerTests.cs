using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Tests.Handlers
{
    [TestFixture]
    public class ReferenceDataHandlerTests
    {
        private AppDbContext _context = null!;
        private ReferenceDataHandler _sut = null!;

        [SetUp]
        public void SetUp()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _sut = new ReferenceDataHandler(_context);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _context.Database.EnsureDeleted();
                _context.Dispose();
            }
            catch
            {
            }
        }

        [Test]
        public async Task AddClassTypeAsync_ValidInput_CreatesClassType()
        {
            DataHandlerResponse<Guid> response = await _sut.AddClassTypeAsync("Wheel Throw", 6);

            Assert.That(response.IsSuccess, Is.True);
            DataHandlerResponse<List<ClassTypeModel>> listResponse = await _sut.GetClassTypesAsync();
            Assert.That(listResponse.Data!.Single().Name, Is.EqualTo("Wheel Throw"));
            Assert.That(listResponse.Data!.Single().MaxCapacity, Is.EqualTo(6));
        }

        [Test]
        public async Task AddClassTypeAsync_DuplicateName_ReturnsFailure()
        {
            await _sut.AddClassTypeAsync("Wheel Throw", 6);

            DataHandlerResponse<Guid> response = await _sut.AddClassTypeAsync("Wheel Throw", 4);

            Assert.That(response.IsSuccess, Is.False);
        }

        [Test]
        public async Task AddClassTypeAsync_ZeroCapacity_ReturnsFailure()
        {
            DataHandlerResponse<Guid> response = await _sut.AddClassTypeAsync("Hand-Building", 0);

            Assert.That(response.IsSuccess, Is.False);
        }

        [Test]
        public async Task UpdateClassTypeCapacityAsync_ValidCapacity_UpdatesRow()
        {
            DataHandlerResponse<Guid> createResponse = await _sut.AddClassTypeAsync("Wheel Throw", 6);

            HandlerResponse updateResponse = await _sut.UpdateClassTypeCapacityAsync(createResponse.Data, 3);

            Assert.That(updateResponse.IsSuccess, Is.True);
            DataHandlerResponse<List<ClassTypeModel>> listResponse = await _sut.GetClassTypesAsync();
            Assert.That(listResponse.Data!.Single().MaxCapacity, Is.EqualTo(3));
        }

        [Test]
        public async Task UpdateClassTypeCapacityAsync_UnknownId_ReturnsNotFound()
        {
            HandlerResponse response = await _sut.UpdateClassTypeCapacityAsync(Guid.NewGuid(), 4);

            Assert.That(response.IsSuccess, Is.False);
        }

        [Test]
        public async Task RemoveClassTypeAsync_ExistingId_RemovesRow()
        {
            DataHandlerResponse<Guid> createResponse = await _sut.AddClassTypeAsync("Hand-Building", 6);

            HandlerResponse removeResponse = await _sut.RemoveClassTypeAsync(createResponse.Data);

            Assert.That(removeResponse.IsSuccess, Is.True);
            DataHandlerResponse<List<ClassTypeModel>> listResponse = await _sut.GetClassTypesAsync();
            Assert.That(listResponse.Data, Is.Empty);
        }

        [Test]
        public async Task EnsureSeedClassTypesAsync_EmptyTable_SeedsDefaults()
        {
            await _sut.EnsureSeedClassTypesAsync();

            DataHandlerResponse<List<ClassTypeModel>> listResponse = await _sut.GetClassTypesAsync();
            Assert.That(listResponse.Data!.Select(c => c.Name), Is.EquivalentTo(new[] { "Wheel Throw", "Hand-Building" }));
            Assert.That(listResponse.Data!.All(c => c.MaxCapacity == 6), Is.True);
        }

        [Test]
        public async Task EnsureSeedClassTypesAsync_TableAlreadyHasRows_DoesNotAddDuplicates()
        {
            await _sut.AddClassTypeAsync("Custom Class", 2);

            await _sut.EnsureSeedClassTypesAsync();

            DataHandlerResponse<List<ClassTypeModel>> listResponse = await _sut.GetClassTypesAsync();
            Assert.That(listResponse.Data, Has.Count.EqualTo(1));
        }
    }
}
