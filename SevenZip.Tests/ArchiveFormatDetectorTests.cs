using System;
using System.IO;
using Xunit;

namespace SevenZip.Tests
{
    public class ArchiveFormatDetectorTests
    {
        private static string ArchivePath(string fileName)
            => Path.Combine(AppContext.BaseDirectory, "TestArchives", fileName);

        [Theory]
        [InlineData("Test.zip",    InArchiveFormat.Zip)]
        [InlineData("Test.tar",    InArchiveFormat.Tar)]
        [InlineData("Test.txt.bz2", InArchiveFormat.BZip2)]
        [InlineData("Test.txt.gz",  InArchiveFormat.GZip)]
        [InlineData("Test.txt.xz",  InArchiveFormat.XZ)]
        [InlineData("Test.lzma.7z", InArchiveFormat.SevenZip)]
        [InlineData("Test.rar",     InArchiveFormat.Rar)]
        public void DetectFormat_BySignature_ReturnsExpectedFormat(string file, InArchiveFormat expected)
        {
            using var fs = new FileStream(ArchivePath(file), FileMode.Open, FileAccess.Read, FileShare.Read);
            var detected = FileChecker.CheckSignature(fs, out _, out _);
            Assert.Equal(expected, detected);
        }

        [Theory]
        [InlineData("Test.zip",    InArchiveFormat.Zip)]
        [InlineData("Test.tar",    InArchiveFormat.Tar)]
        [InlineData("Test.txt.bz2", InArchiveFormat.BZip2)]
        [InlineData("Test.txt.gz",  InArchiveFormat.GZip)]
        [InlineData("Test.txt.xz",  InArchiveFormat.XZ)]
        [InlineData("Test.lzma.7z", InArchiveFormat.SevenZip)]
        public void DetectFormat_ByFileName_ReturnsExpectedFormat(string file, InArchiveFormat expected)
        {
            var detected = FileChecker.CheckSignature(ArchivePath(file), out _, out _);
            Assert.Equal(expected, detected);
        }
    }
}
