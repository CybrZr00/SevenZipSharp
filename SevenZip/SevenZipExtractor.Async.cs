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
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    partial class SevenZipExtractor
    {
        // -----------------------------------------------------------------------
        // Task-based async wrappers (replaces the old BeginInvoke/EndInvoke API)
        // Each method captures the caller's SynchronizationContext so that events
        // raised during extraction are marshalled back to the original thread.
        // -----------------------------------------------------------------------

        private Task RunAsync(System.Action action, CancellationToken ct)
        {
            SaveContext();
            _asynchronousDisposeLock = true;
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                try { action(); }
                finally
                {
                    _asynchronousDisposeLock = false;
                    ReleaseContext();
                }
            }, ct);
        }

        /// <summary>Unpacks the whole archive asynchronously to the specified directory.</summary>
        public Task ExtractArchiveAsync(string directory, CancellationToken ct = default)
            => RunAsync(() => ExtractArchive(directory), ct);

        /// <summary>Unpacks a file asynchronously by its name to the specified stream.</summary>
        public Task ExtractFileAsync(string fileName, Stream stream, CancellationToken ct = default)
            => RunAsync(() => ExtractFile(fileName, stream), ct);

        /// <summary>Unpacks a file asynchronously by its index to the specified stream.</summary>
        public Task ExtractFileAsync(int index, Stream stream, CancellationToken ct = default)
            => RunAsync(() => ExtractFile(index, stream), ct);

        /// <summary>Unpacks files asynchronously by their indices to the specified directory.</summary>
        public Task ExtractFilesAsync(string directory, CancellationToken ct = default, params int[] indexes)
            => RunAsync(() => ExtractFiles(directory, indexes), ct);

        /// <summary>Unpacks files asynchronously by their full names to the specified directory.</summary>
        public Task ExtractFilesAsync(string directory, CancellationToken ct = default, params string[] fileNames)
            => RunAsync(() => ExtractFiles(directory, fileNames), ct);

        /// <summary>
        /// Extracts files from the archive asynchronously, giving a callback the choice what
        /// to do with each file. The order of the files is given by the archive.
        /// 7-Zip (and any other solid) archives are NOT supported.
        /// </summary>
        public Task ExtractFilesAsync(ExtractFileCallback extractFileCallback, CancellationToken ct = default)
            => RunAsync(() => ExtractFiles(extractFileCallback), ct);
    }
}
