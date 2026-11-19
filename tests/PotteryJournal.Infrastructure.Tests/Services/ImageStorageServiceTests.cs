using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PotteryJournal.Infrastructure.Constants;
using PotteryJournal.Infrastructure.Options;
using PotteryJournal.Infrastructure.Services;
using PotteryJournal.SharedKernel.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PotteryJournal.Infrastructure.Tests.Services
{
    [TestFixture]
    public class ImageStorageServiceTests
    {
        private const int THUMBNAIL_SHORT_EDGE = 600;

        private string _rootPath = string.Empty;
        private ImageStorageService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _rootPath = Path.Combine(Path.GetTempPath(), $"pj-uploads-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_rootPath);
            _sut = new ImageStorageService(Microsoft.Extensions.Options.Options.Create(new UploadsOptions
            {
                RootPath = _rootPath,
            }));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Directory.Delete(_rootPath, true);
            }
            catch
            {
            }
        }

        [Test]
        public async Task SaveResizedJpegAsync_LargePhoto_SavesThumbnailWithShortEdgeLimit()
        {
            using MemoryStream upload = BuildJpeg(3000, 2000);

            DataHandlerResponse<string> response = await _sut.SaveResizedJpegAsync(upload, UploadsSubfolders.Pieces);

            Assert.That(response.IsSuccess, Is.True);
            ImageInfo full = await Image.IdentifyAsync(FullPath(response.Data!));
            ImageInfo thumbnail = await Image.IdentifyAsync(ThumbnailPath(response.Data!));
            Assert.That(full.Width, Is.EqualTo(1600));
            Assert.That(thumbnail.Height, Is.EqualTo(THUMBNAIL_SHORT_EDGE));
            Assert.That(thumbnail.Width, Is.EqualTo(900));
        }

        [Test]
        public async Task SaveResizedJpegAsync_SmallPhoto_ThumbnailNotUpscaled()
        {
            using MemoryStream upload = BuildJpeg(400, 300);

            DataHandlerResponse<string> response = await _sut.SaveResizedJpegAsync(upload, UploadsSubfolders.Events);

            ImageInfo thumbnail = await Image.IdentifyAsync(ThumbnailPath(response.Data!, UploadsSubfolders.Events));
            Assert.That(thumbnail.Width, Is.EqualTo(400));
            Assert.That(thumbnail.Height, Is.EqualTo(300));
        }

        [Test]
        public async Task Delete_SavedPhoto_RemovesPhotoAndThumbnail()
        {
            using MemoryStream upload = BuildJpeg(800, 800);
            DataHandlerResponse<string> saved = await _sut.SaveResizedJpegAsync(upload, UploadsSubfolders.Pieces);

            HandlerResponse response = _sut.Delete(UploadsSubfolders.Pieces, saved.Data!);

            Assert.That(response.IsSuccess, Is.True);
            Assert.That(File.Exists(FullPath(saved.Data!)), Is.False);
            Assert.That(File.Exists(ThumbnailPath(saved.Data!)), Is.False);
        }

        [Test]
        public async Task CreateMissingThumbnailsAsync_PhotosWithoutThumbnails_CreatesOnlyMissingOnes()
        {
            string piecesFolder = Path.Combine(_rootPath, UploadsSubfolders.Pieces);
            Directory.CreateDirectory(piecesFolder);
            await WriteJpegAsync(Path.Combine(piecesFolder, "old-one.jpg"), 1200, 1600);
            await WriteJpegAsync(Path.Combine(piecesFolder, "old-two.jpg"), 1600, 1200);
            using MemoryStream upload = BuildJpeg(1000, 1000);
            DataHandlerResponse<string> alreadyThumbed = await _sut.SaveResizedJpegAsync(upload, UploadsSubfolders.Pieces);

            DataHandlerResponse<int> response = await _sut.CreateMissingThumbnailsAsync(UploadsSubfolders.Pieces, CancellationToken.None);

            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Data, Is.EqualTo(2));
            Assert.That(File.Exists(ThumbnailPath("old-one.jpg")), Is.True);
            Assert.That(File.Exists(ThumbnailPath("old-two.jpg")), Is.True);
            Assert.That(File.Exists(ThumbnailPath(alreadyThumbed.Data!)), Is.True);
            Assert.That(Directory.GetFiles(Path.Combine(piecesFolder, UploadsSubfolders.Thumbnails), "*.tmp"), Is.Empty);
        }

        [Test]
        public async Task CreateMissingThumbnailsAsync_SecondRun_CreatesNothing()
        {
            string piecesFolder = Path.Combine(_rootPath, UploadsSubfolders.Pieces);
            Directory.CreateDirectory(piecesFolder);
            await WriteJpegAsync(Path.Combine(piecesFolder, "old.jpg"), 900, 900);
            _ = await _sut.CreateMissingThumbnailsAsync(UploadsSubfolders.Pieces, CancellationToken.None);

            DataHandlerResponse<int> response = await _sut.CreateMissingThumbnailsAsync(UploadsSubfolders.Pieces, CancellationToken.None);

            Assert.That(response.Data, Is.EqualTo(0));
        }

        [Test]
        public async Task CreateMissingThumbnailsAsync_UnreadableFile_WarnsAndContinues()
        {
            string piecesFolder = Path.Combine(_rootPath, UploadsSubfolders.Pieces);
            Directory.CreateDirectory(piecesFolder);
            await File.WriteAllTextAsync(Path.Combine(piecesFolder, "broken.jpg"), "not an image");
            await WriteJpegAsync(Path.Combine(piecesFolder, "good.jpg"), 900, 900);

            DataHandlerResponse<int> response = await _sut.CreateMissingThumbnailsAsync(UploadsSubfolders.Pieces, CancellationToken.None);

            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Data, Is.EqualTo(1));
            Assert.That(response.Warnings, Has.Count.EqualTo(1));
            Assert.That(response.Warnings[0], Does.Contain("broken.jpg"));
        }

        [Test]
        public async Task CreateMissingThumbnailsAsync_FolderMissing_ReturnsZero()
        {
            DataHandlerResponse<int> response = await _sut.CreateMissingThumbnailsAsync(UploadsSubfolders.Events, CancellationToken.None);

            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Data, Is.EqualTo(0));
        }

        private string FullPath(string fileName)
        {
            return Path.Combine(_rootPath, UploadsSubfolders.Pieces, fileName);
        }

        private string ThumbnailPath(string fileName)
        {
            return ThumbnailPath(fileName, UploadsSubfolders.Pieces);
        }

        private string ThumbnailPath(string fileName, string subfolder)
        {
            return Path.Combine(_rootPath, subfolder, UploadsSubfolders.Thumbnails, fileName);
        }

        private static MemoryStream BuildJpeg(int width, int height)
        {
            MemoryStream stream = new MemoryStream();
            using (Image<Rgb24> image = new Image<Rgb24>(width, height))
            {
                image.SaveAsJpeg(stream);
            }

            stream.Position = 0;
            return stream;
        }

        private static async Task WriteJpegAsync(string path, int width, int height)
        {
            using (Image<Rgb24> image = new Image<Rgb24>(width, height))
            {
                await image.SaveAsJpegAsync(path);
            }
        }
    }
}
