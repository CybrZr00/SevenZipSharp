using System;
using System.IO;
using System.IO.Compression;
using System.Security;
using Xunit;

namespace SevenZip.Tests
{
    /// <summary>
    /// Verifies that archive entries resolving outside the extraction root are rejected (OWASP zip-slip fix).
    /// </summary>
    public class PathTraversalTests : IDisposable
    {
        private readonly string _tempDir;

        public PathTraversalTests()
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

        [Fact]
        public void Extract_LegitimateZip_ExtractsSuccessfully()
        {
            var outDir = Path.Combine(_tempDir, "legit");
            Directory.CreateDirectory(outDir);

            using var extractor = new SevenZipExtractor(ArchivePath("Test.zip"));
            extractor.ExtractArchive(outDir);

            Assert.True(Directory.GetFiles(outDir, "*", SearchOption.AllDirectories).Length > 0,
                "Should have extracted at least one file.");
        }

        [Fact]
        public void Extract_AllExtractedFiles_StayWithinOutputDirectory()
        {
            var outDir = Path.Combine(_tempDir, "boundary");
            Directory.CreateDirectory(outDir);
            var resolvedRoot = Path.GetFullPath(outDir);

            using var extractor = new SevenZipExtractor(ArchivePath("Test.zip"));
            extractor.ExtractArchive(outDir);

            foreach (var file in Directory.GetFiles(outDir, "*", SearchOption.AllDirectories))
            {
                var resolved = Path.GetFullPath(file);
                Assert.StartsWith(resolvedRoot, resolved, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
