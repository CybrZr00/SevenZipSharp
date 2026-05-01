/*  This file is part of SevenZipSharp.

    SevenZipSharp is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SevenZipSharp is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with SevenZipSharp.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace SevenZip
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    partial class SevenZipCompressor
    {
        // -----------------------------------------------------------------------
        // Task-based async wrappers (replaces the old BeginInvoke/EndInvoke API)
        // Each method captures the caller's SynchronizationContext so that events
        // raised during compression are marshalled back to the original thread.
        // -----------------------------------------------------------------------

        private Task RunAsync(System.Action action, CancellationToken ct)
        {
            SaveContext();
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                try { action(); }
                finally { ReleaseContext(); }
            }, ct);
        }

        /// <summary>Packs files into the archive asynchronously.</summary>
        public Task CompressFilesAsync(string archiveName, CancellationToken ct = default, params string[] fileFullNames)
            => RunAsync(() => CompressFiles(archiveName, fileFullNames), ct);

        /// <summary>Packs files into the archive asynchronously.</summary>
        public Task CompressFilesAsync(Stream archiveStream, CancellationToken ct = default, params string[] fileFullNames)
            => RunAsync(() => CompressFiles(archiveStream, fileFullNames), ct);

        /// <summary>Packs files into the archive asynchronously.</summary>
        public Task CompressFilesAsync(string archiveName, int commonRootLength, CancellationToken ct = default, params string[] fileFullNames)
            => RunAsync(() => CompressFiles(archiveName, commonRootLength, fileFullNames), ct);

        /// <summary>Packs files into the archive asynchronously.</summary>
        public Task CompressFilesAsync(Stream archiveStream, int commonRootLength, CancellationToken ct = default, params string[] fileFullNames)
            => RunAsync(() => CompressFiles(archiveStream, commonRootLength, fileFullNames), ct);

        /// <summary>Packs encrypted files into the archive asynchronously.</summary>
        public Task CompressFilesEncryptedAsync(string archiveName, string password, CancellationToken ct = default, params string[] fileFullNames)
            => RunAsync(() => CompressFilesEncrypted(archiveName, password, fileFullNames), ct);

        /// <summary>Packs encrypted files into the archive asynchronously.</summary>
        public Task CompressFilesEncryptedAsync(Stream archiveStream, string password, CancellationToken ct = default, params string[] fileFullNames)
            => RunAsync(() => CompressFilesEncrypted(archiveStream, password, fileFullNames), ct);

        /// <summary>Packs encrypted files into the archive asynchronously.</summary>
        public Task CompressFilesEncryptedAsync(string archiveName, int commonRootLength, string password, CancellationToken ct = default, params string[] fileFullNames)
            => RunAsync(() => CompressFilesEncrypted(archiveName, commonRootLength, password, fileFullNames), ct);

        /// <summary>Packs encrypted files into the archive asynchronously.</summary>
        public Task CompressFilesEncryptedAsync(Stream archiveStream, int commonRootLength, string password, CancellationToken ct = default, params string[] fileFullNames)
            => RunAsync(() => CompressFilesEncrypted(archiveStream, commonRootLength, password, fileFullNames), ct);

        /// <summary>Recursively packs all files in the specified directory asynchronously.</summary>
        public Task CompressDirectoryAsync(string directory, string archiveName,
            string password = "", string searchPattern = "*", bool recursion = true,
            CancellationToken ct = default)
            => RunAsync(() => CompressDirectory(directory, archiveName, password, searchPattern, recursion), ct);

        /// <summary>Recursively packs all files in the specified directory asynchronously.</summary>
        public Task CompressDirectoryAsync(string directory, Stream archiveStream,
            string password = "", string searchPattern = "*", bool recursion = true,
            CancellationToken ct = default)
            => RunAsync(() => CompressDirectory(directory, archiveStream, password, searchPattern, recursion), ct);

        /// <summary>Compresses the specified stream asynchronously.</summary>
        public Task CompressStreamAsync(Stream inStream, Stream outStream, CancellationToken ct = default)
            => RunAsync(() => CompressStream(inStream, outStream), ct);

        /// <summary>Compresses the specified stream asynchronously.</summary>
        public Task CompressStreamAsync(Stream inStream, Stream outStream, string password, CancellationToken ct = default)
            => RunAsync(() => CompressStream(inStream, outStream, password), ct);

        /// <summary>Modifies the existing archive asynchronously (renames or deletes files).</summary>
        public Task ModifyArchiveAsync(string archiveName, Dictionary<int, string> newFileNames,
            string password = "", CancellationToken ct = default)
            => RunAsync(() => ModifyArchive(archiveName, newFileNames, password), ct);
    }
}
