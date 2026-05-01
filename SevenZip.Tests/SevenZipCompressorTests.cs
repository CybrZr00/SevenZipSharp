using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SevenZip.Tests
{
    public class SevenZipCompressorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _sampleFile;

        public SevenZipCompressorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SevenZipTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _sampleFile = Path.Combine(_tempDir, "sample.txt");
            File.WriteAllText(_sampleFile, "Hello from SevenZipSharp test!\nSecond line.\n");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        private string ArchivePath(string name) => Path.Combine(_tempDir, name);

        [Fact]
        public void Compress_ToSevenZip_CreatesArchive()
        {
            var archive = ArchivePath("out.7z");
            var compressor = new SevenZipCompressor { ArchiveFormat = OutArchiveFormat.SevenZip };
            compressor.CompressFiles(archive, _sampleFile);
            Assert.True(File.Exists(archive), "7z archive should be created.");
            Assert.True(new FileInfo(archive).Length > 0);
        }

        [Fact]
        public void Compress_ToZip_CreatesArchive()
        {
            var archive = ArchivePath("out.zip");
            var compressor = new SevenZipCompressor { ArchiveFormat = OutArchiveFormat.Zip };
            compressor.CompressFiles(archive, _sampleFile);
            Assert.True(File.Exists(archive));
            Assert.True(new FileInfo(archive).Length > 0);
        }

        [Fact]
        public void Compress_ToBZip2_CreatesArchive()
        {
            var archive = ArchivePath("out.bz2");
            var compressor = new SevenZipCompressor { ArchiveFormat = OutArchiveFormat.BZip2 };
            compressor.CompressFiles(archive, _sampleFile);
            Assert.True(File.Exists(archive));
        }

        [Fact]
        public void Compress_ToGZip_CreatesArchive()
        {
            var archive = ArchivePath("out.gz");
            var compressor = new SevenZipCompressor { ArchiveFormat = OutArchiveFormat.GZip };
            compressor.CompressFiles(archive, _sampleFile);
            Assert.True(File.Exists(archive));
        }

        [Fact]
        public void Compress_ToXz_CreatesArchive()
        {
            var archive = ArchivePath("out.xz");
            var compressor = new SevenZipCompressor { ArchiveFormat = OutArchiveFormat.XZ };
            compressor.CompressFiles(archive, _sampleFile);
            Assert.True(File.Exists(archive), "XZ compression regression: archive should be created after LZMA2 codec fix.");
            Assert.True(new FileInfo(archive).Length > 0);
        }

        [Fact]
        public void Compress_ToSevenZipWithPpmd_CreatesArchive()
        {
            var archive = ArchivePath("out.ppmd.7z");
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                CompressionMethod = CompressionMethod.Ppmd
            };
            compressor.CompressFiles(archive, _sampleFile);
            Assert.True(File.Exists(archive), "PPMd compression regression: archive should be created after PPMd method fix.");
            Assert.True(new FileInfo(archive).Length > 0);
        }

        [Fact]
        public void CompressDirectory_ToSevenZip_CreatesArchive()
        {
            var srcDir = Path.Combine(_tempDir, "srcdir");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(srcDir, "a.txt"), "file a");
            File.WriteAllText(Path.Combine(srcDir, "b.txt"), "file b");

            var archive = ArchivePath("dir.7z");
            var compressor = new SevenZipCompressor { ArchiveFormat = OutArchiveFormat.SevenZip };
            compressor.CompressDirectory(srcDir, archive);
            Assert.True(File.Exists(archive));
        }

        [Fact]
        public async Task CompressFilesAsync_CompletesSuccessfully()
        {
            var archive = ArchivePath("async.7z");
            var compressor = new SevenZipCompressor { ArchiveFormat = OutArchiveFormat.SevenZip };
            await compressor.CompressFilesAsync(archive, fileFullNames: new[] { _sampleFile });
            Assert.True(File.Exists(archive));
        }

        [Fact]
        public void CompressAndExtract_RoundTrip_ProducesOriginalContent()
        {
            var archive = ArchivePath("roundtrip.7z");
            var outDir = Path.Combine(_tempDir, "out");
            Directory.CreateDirectory(outDir);

            var compressor = new SevenZipCompressor { ArchiveFormat = OutArchiveFormat.SevenZip };
            compressor.CompressFiles(archive, _sampleFile);

            using var extractor = new SevenZipExtractor(archive);
            extractor.ExtractArchive(outDir);

            var extractedFiles = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories);
            Assert.Single(extractedFiles);
            Assert.Equal(File.ReadAllText(_sampleFile), File.ReadAllText(extractedFiles[0]));
        }
    }
}
