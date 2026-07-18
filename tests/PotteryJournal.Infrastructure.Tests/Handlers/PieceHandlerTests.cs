using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PotteryJournal.Infrastructure.Data;
using PotteryJournal.Infrastructure.Data.Entities;
using PotteryJournal.Infrastructure.Handlers;
using PotteryJournal.Infrastructure.Models;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Tests.Handlers
{
    [TestFixture]
    public class PieceHandlerTests
    {
        private AppDbContext _context = null!;
        private PieceHandler _sut = null!;
        private Guid _clayBodyId;
        private Guid _glazeId;

        [SetUp]
        public void SetUp()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _sut = new PieceHandler(_context);

            ClayBody clayBody = new ClayBody { Id = Guid.NewGuid(), Name = "Stoneware" };
            Glaze glaze = new Glaze { Id = Guid.NewGuid(), Name = "Ironfall Blue" };
            _context.ClayBodies.Add(clayBody);
            _context.Glazes.Add(glaze);
            _context.SaveChanges();
            _clayBodyId = clayBody.Id;
            _glazeId = glaze.Id;
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
        public async Task CreateAsync_FirstPiece_AssignsPieceNumberOne()
        {
            DataHandlerResponse<Guid> response = await _sut.CreateAsync(BuildSaveModel("First Piece"), "admin@example.com");

            Assert.That(response.IsSuccess, Is.True);
            DataHandlerResponse<PieceDetailModel> detail = await _sut.GetByIdAsync(response.Data);
            Assert.That(detail.Data!.PieceNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task CreateAsync_SecondPiece_AssignsNextSequentialPieceNumber()
        {
            await _sut.CreateAsync(BuildSaveModel("First Piece"), "admin@example.com");
            DataHandlerResponse<Guid> secondResponse = await _sut.CreateAsync(BuildSaveModel("Second Piece"), "admin@example.com");

            DataHandlerResponse<PieceDetailModel> detail = await _sut.GetByIdAsync(secondResponse.Data);
            Assert.That(detail.Data!.PieceNumber, Is.EqualTo(2));
        }

        [Test]
        public async Task GetAllDetailsAsync_FilteredByCategory_ReturnsOnlyMatchingPieces()
        {
            Category bowlsCategory = new Category { Id = Guid.NewGuid(), Name = "Bowls" };
            Category vasesCategory = new Category { Id = Guid.NewGuid(), Name = "Vases" };
            _context.Categories.AddRange(bowlsCategory, vasesCategory);
            await _context.SaveChangesAsync();

            PieceSaveModel bowl = BuildSaveModel("Bowl One");
            bowl.CategoryId = bowlsCategory.Id;
            bowl.GlazeApplications.Add(new GlazeApplicationModel { Location = "Interior", GlazeId = _glazeId, Coats = 2 });
            PieceSaveModel vase = BuildSaveModel("Vase One");
            vase.CategoryId = vasesCategory.Id;
            vase.GlazeApplications.Add(new GlazeApplicationModel { Location = "Interior", GlazeId = _glazeId, Coats = 2 });
            await _sut.CreateAsync(bowl, "admin@example.com");
            await _sut.CreateAsync(vase, "admin@example.com");

            DataHandlerResponse<List<PieceDetailModel>> response = await _sut.GetAllDetailsAsync("Bowls");

            Assert.That(response.Data, Has.Count.EqualTo(1));
            Assert.That(response.Data![0].Title, Is.EqualTo("Bowl One"));
        }

        [Test]
        public async Task UpdateAsync_ReplacesNotesAndGlazeApplications()
        {
            DataHandlerResponse<Guid> createResponse = await _sut.CreateAsync(BuildSaveModel("Mug"), "admin@example.com");

            PieceSaveModel updateModel = BuildSaveModel("Mug");
            updateModel.Notes.Add(new PieceNoteModel { Title = "Firing", NoteText = "Cone 6" });
            updateModel.GlazeApplications.Add(new GlazeApplicationModel { Location = "Interior", GlazeId = _glazeId, Coats = 2 });

            await _sut.UpdateAsync(createResponse.Data, updateModel);

            DataHandlerResponse<PieceDetailModel> detail = await _sut.GetByIdAsync(createResponse.Data);
            Assert.That(detail.Data!.Notes, Has.Count.EqualTo(1));
            Assert.That(detail.Data!.GlazeApplications, Has.Count.EqualTo(1));
            Assert.That(detail.Data!.GlazeApplications[0].GlazeName, Is.EqualTo("Ironfall Blue"));
        }

        [Test]
        public async Task DeleteAsync_NonExistentId_ReturnsFailure()
        {
            HandlerResponse response = await _sut.DeleteAsync(Guid.NewGuid());

            Assert.That(response.IsSuccess, Is.False);
        }

        private PieceSaveModel BuildSaveModel(string title)
        {
            return new PieceSaveModel
            {
                Title = title,
                ClayBodyId = _clayBodyId,
                StartedDate = new DateOnly(2026, 1, 1),
                SizeText = "6\" x 4\"",
                WeightText = "1.2 lb",
                GlazeSummary = "Clear over white slip",
            };
        }
    }
}
