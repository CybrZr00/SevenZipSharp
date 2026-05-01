using System;
using System.IO;
using System.Linq;
using Xunit;

namespace SevenZip.Tests
{
    public class SevenZipExtractorTests : IDisposable
    {
        private readonly string _tempDir;

        public SevenZipExtractorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SevenZipTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        private static string ArchivePath(string fileName)
            => Path.Combine(AppContext.BaseDirectory, "TestArchives", fileName);

        private string ExtractTo(string archiveFile, string subfolder)
        {
            var outDir = Path.Combine(_tempDir, subfolder);
            Directory.CreateDirectory(outDir);
            using var extractor = new SevenZipExtractor(ArchivePath(archiveFile));
            extractor.ExtractArchive(outDir);
            return outDir;
        }

        [Fact]
        public void ExtractZip_ProducesFiles()
        {
            var dir = ExtractTo("Test.zip", "zip");
            Assert.True(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length > 0);
        }

        [Fact]
        public void ExtractTar_ProducesFiles()
        {
            var dir = ExtractTo("Test.tar", "tar");
            Assert.True(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length > 0);
        }

        [Fact]
        public void ExtractBzip2_ProducesFiles()
        {
            var dir = ExtractTo("Test.txt.bz2", "bzip2");
            Assert.True(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length > 0);
        }

        [Fact]
        public void ExtractGzip_ProducesFiles()
        {
            var dir = ExtractTo("Test.txt.gz", "gzip");
            Assert.True(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length > 0);
        }

        [Fact]
        public void ExtractXz_ProducesFiles()
        {
            var dir = ExtractTo("Test.txt.xz", "xz");
            Assert.True(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length > 0,
                "XZ extraction regression: extraction should succeed after XZ LZMA2 codec fix.");
        }

        [Fact]
        public void Extract7zLzma_ProducesFiles()
        {
            var dir = ExtractTo("Test.lzma.7z", "7z-lzma");
            Assert.True(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length > 0);
        }

        [Fact]
        public void Extract7zLzma2_ProducesFiles()
        {
            var dir = ExtractTo("Test.lzma2.7z", "7z-lzma2");
            Assert.True(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length > 0);
        }

        [Fact]
        public void Extract7zPpmd_ProducesFiles()
        {
            var dir = ExtractTo("Test.ppmd.7z", "7z-ppmd");
            Assert.True(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length > 0,
                "PPMd extraction regression: extraction should succeed after PPMd method name fix.");
        }

        [Fact]
        public void ExtractToStream_FileByIndex_ReturnsContent()
        {
            using var extractor = new SevenZipExtractor(ArchivePath("Test.zip"));
            using var ms = new MemoryStream();
            extractor.ExtractFile(0, ms);
            Assert.True(ms.Length > 0);
        }

        [Fact]
        public void ArchiveFileData_ReturnsEntries()
        {
            using var extractor = new SevenZipExtractor(ArchivePath("Test.zip"));
            var entries = extractor.ArchiveFileData;
            Assert.NotEmpty(entries);
        }

        [Fact]
        public async System.Threading.Tasks.Task ExtractArchiveAsync_CompletesSuccessfully()
        {
            var outDir = Path.Combine(_tempDir, "async-zip");
            Directory.CreateDirectory(outDir);
            using var extractor = new SevenZipExtractor(ArchivePath("Test.zip"));
            await extractor.ExtractArchiveAsync(outDir);
            Assert.True(Directory.GetFiles(outDir, "*", SearchOption.AllDirectories).Length > 0);
        }
    }
}
