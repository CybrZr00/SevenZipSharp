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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SevenZip
{
#if UNMANAGED

    /// <summary>
    /// The way of the event synchronization.
    /// </summary>
    public enum EventSynchronizationStrategy
    {
        /// <summary>
        /// Events are called synchronously if user can do some action; that is, cancel the execution process for example.
        /// </summary>
        Default,
        /// <summary>
        /// Always call events asynchronously.
        /// </summary>
        AlwaysAsynchronous,
        /// <summary>
        /// Always call events synchronously.
        /// </summary>
        AlwaysSynchronous
    }

    /// <summary>
    /// SevenZip Extractor/Compressor base class. Implements Password string, ReportErrors flag.
    /// </summary>
    public abstract class SevenZipBase
    {
        private readonly string _password;
        private readonly bool _reportErrors;
        private readonly int _uniqueID;
        private static int IncrementingUniqueId = Environment.TickCount;

        private ILogger _logger = NullLogger.Instance;

        /// <summary>
        /// Gets or sets the event synchronization strategy.
        /// </summary>
        public EventSynchronizationStrategy EventSynchronization { get; set; }

        /// <summary>
        /// The SynchronizationContext captured when async operations begin, used to marshal events back to the caller's context.
        /// </summary>
        internal SynchronizationContext? Context { get; set; }

        /// <summary>
        /// True if the instance of the class needs to be recreated in new thread context; otherwise, false.
        /// </summary>
        protected internal bool NeedsToBeRecreated;

        internal virtual void SaveContext()
        {
            Context = SynchronizationContext.Current;
            NeedsToBeRecreated = true;
        }

        internal virtual void ReleaseContext()
        {
            Context = null;
            NeedsToBeRecreated = true;
            // GC.SuppressFinalize remains for any subclass that adds a finalizer
            GC.SuppressFinalize(this);
        }

        private delegate void EventHandlerDelegate<T>(EventHandler<T> handler, T e) where T : EventArgs;

        internal void OnEvent<T>(EventHandler<T>? handler, T e, bool synchronous) where T : EventArgs
        {
            if (handler == null) return;
            try
            {
                switch (EventSynchronization)
                {
                    case EventSynchronizationStrategy.AlwaysAsynchronous:
                        synchronous = false;
                        break;
                    case EventSynchronizationStrategy.AlwaysSynchronous:
                        synchronous = true;
                        break;
                }

                if (Context == null)
                {
                    handler(this, e);
                }
                else
                {
                    var callback = new SendOrPostCallback(obj =>
                    {
                        var array = (object[])obj!;
                        ((EventHandler<T>)array[0])(array[1], (T)array[2]);
                    });
                    if (synchronous)
                        Context.Send(callback, new object[] { handler, this, e });
                    else
                        Context.Post(callback, new object[] { handler, this, e });
                }
            }
            catch (Exception ex)
            {
                AddException(ex);
            }
        }

        /// <summary>
        /// Gets the unique identifier of this SevenZipBase instance.
        /// </summary>
        public int UniqueID => _uniqueID;

        /// <summary>
        /// User exceptions thrown during the requested operations, for example, in events.
        /// </summary>
        private readonly List<Exception> _exceptions = new List<Exception>();

        private static int GetUniqueID()
        {
            return Interlocked.Increment(ref IncrementingUniqueId);
        }

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the SevenZipBase class.
        /// </summary>
        protected SevenZipBase()
        {
            _password = "";
            _reportErrors = true;
            _uniqueID = GetUniqueID();
        }

        /// <summary>
        /// Initializes a new instance of the SevenZipBase class.
        /// </summary>
        /// <param name="password">The archive password.</param>
        protected SevenZipBase(string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                throw new SevenZipException("Empty password was specified.");
            }
            _password = password;
            _reportErrors = true;
            _uniqueID = GetUniqueID();
        }
        #endregion

        /// <summary>
        /// Configures a logger for this instance. Logged events include suppressed exceptions.
        /// </summary>
        /// <param name="logger">The logger to use. Pass null to disable logging.</param>
        public void ConfigureLogging(ILogger? logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Gets the logger for this instance.
        /// </summary>
        internal ILogger Logger => _logger;

        /// <summary>
        /// Gets the archive password.
        /// </summary>
        /// <remarks>
        /// SECURITY NOTE: The password is stored as a plain <see cref="string"/>. Strings in .NET are
        /// immutable and cannot be zeroed from managed code; the password may persist in GC memory until
        /// collected. For high-security applications, consider clearing the password after use by
        /// setting the field to an empty string.
        /// </remarks>
        public string Password => _password;

        /// <summary>
        /// Gets throw exceptions on archive errors flag
        /// </summary>
        internal bool ReportErrors => _reportErrors;

        /// <summary>
        /// Gets the user exceptions thrown during the requested operations, for example, in events.
        /// </summary>
        internal ReadOnlyCollection<Exception> Exceptions => new ReadOnlyCollection<Exception>(_exceptions);

        internal void AddException(Exception e)
        {
            _exceptions.Add(e);
        }

        internal void ClearExceptions()
        {
            _exceptions.Clear();
        }

        internal bool HasExceptions => _exceptions.Count > 0;

        /// <summary>
        /// Throws the specified exception when able to.
        /// </summary>
        /// <param name="e">The exception to throw.</param>
        /// <param name="handler">The handler responsible for the exception.</param>
        internal bool ThrowException(CallbackBase? handler, params Exception[] e)
        {
            if (_reportErrors && (handler == null || !handler.Canceled))
            {
                throw e[0];
            }
            return false;
        }

        internal void ThrowUserException()
        {
            if (HasExceptions)
            {
                throw new SevenZipException(SevenZipException.USER_EXCEPTION_MESSAGE);
            }
        }

        /// <summary>
        /// Throws exception if HRESULT != 0.
        /// </summary>
        /// <param name="hresult">Result code to check.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="handler">The class responsible for the callback.</param>
        internal void CheckedExecute(int hresult, string message, CallbackBase handler)
        {
            if (hresult != (int)OperationResult.Ok || handler.HasExceptions)
            {
                if (!handler.HasExceptions)
                {
                    if (hresult < -2000000000)
                    {
                        ThrowException(handler,
                                       new SevenZipException(
                                           "The execution has failed due to the bug in the SevenZipSharp. (code: " + hresult.ToString() + ")"));
                    }
                    else
                    {
                        ThrowException(handler,
                                       new SevenZipException(message + " hresult=" + hresult.ToString(CultureInfo.InvariantCulture) +
                                                             '.'));
                    }
                }
                else
                {
                    ThrowException(handler, handler.Exceptions[0]);
                }
            }
        }

        /// <summary>
        /// Changes the path to the 7-zip native library.
        /// </summary>
        /// <param name="libraryPath">The path to the 7-zip native library.</param>
        public static void SetLibraryPath(string libraryPath)
        {
            SevenZipLibraryManager.SetLibraryPath(libraryPath);
        }

        /// <summary>
        /// Returns the version information of the native 7zip library.
        /// </summary>
        /// <returns>An object representing the version information of the native 7zip library.</returns>
        [CLSCompliant(false)]
        public static FileVersionInfo GetLibraryVersion()
        {
            return SevenZipLibraryManager.GetLibraryVersion();
        }

        /// <summary>
        /// Gets the current library features.
        /// </summary>
        [CLSCompliant(false)]
        public static LibraryFeature CurrentLibraryFeatures => SevenZipLibraryManager.CurrentLibraryFeatures;

        /// <summary>
        /// Determines whether the specified System.Object is equal to the current SevenZipBase.
        /// </summary>
        public override bool Equals(object? obj)
        {
            var inst = obj as SevenZipBase;
            if (inst == null)
            {
                return false;
            }
            return _uniqueID == inst._uniqueID;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        public override int GetHashCode()
        {
            return _uniqueID;
        }

        /// <summary>
        /// Returns a System.String that represents the current SevenZipBase.
        /// </summary>
        public override string ToString()
        {
            var type = "SevenZipBase";
            if (this is SevenZipExtractor)
            {
                type = "SevenZipExtractor";
            }
            if (this is SevenZipCompressor)
            {
                type = "SevenZipCompressor";
            }
            return string.Format("{0} [{1}]", type, _uniqueID);
        }
    }

    internal class CallbackBase
    {
        private readonly string _password;
        private readonly bool _reportErrors;
        private readonly List<Exception> _exceptions = new List<Exception>();

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the CallbackBase class.
        /// </summary>
        protected CallbackBase()
        {
            _password = "";
            _reportErrors = true;
        }

        /// <summary>
        /// Initializes a new instance of the CallbackBase class.
        /// </summary>
        /// <param name="password">The archive password.</param>
        protected CallbackBase(string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                throw new SevenZipException("Empty password was specified.");
            }
            _password = password;
            _reportErrors = true;
        }
        #endregion

        /// <summary>
        /// Gets the archive password.
        /// </summary>
        public string Password => _password;

        /// <summary>
        /// Gets or sets the value indicating whether the current procedure was cancelled.
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Gets throw exceptions on archive errors flag.
        /// </summary>
        public bool ReportErrors => _reportErrors;

        /// <summary>
        /// Gets the user exceptions thrown during the requested operations, for example, in events.
        /// </summary>
        public ReadOnlyCollection<Exception> Exceptions => new ReadOnlyCollection<Exception>(_exceptions);

        public void AddException(Exception e)
        {
            _exceptions.Add(e);
        }

        public void ClearExceptions()
        {
            _exceptions.Clear();
        }

        public bool HasExceptions => _exceptions.Count > 0;

        /// <summary>
        /// Throws the specified exception when able to.
        /// </summary>
        public bool ThrowException(CallbackBase? handler, params Exception[] e)
        {
            if (_reportErrors && (handler == null || !handler.Canceled))
            {
                throw e[0];
            }
            return false;
        }

        /// <summary>
        /// Throws the first exception in the list if any exists.
        /// </summary>
        /// <returns>True means no exceptions.</returns>
        public bool ThrowException()
        {
            if (HasExceptions && _reportErrors)
            {
                throw _exceptions[0];
            }
            return true;
        }

        public void ThrowUserException()
        {
            if (HasExceptions)
            {
                throw new SevenZipException(SevenZipException.USER_EXCEPTION_MESSAGE);
            }
        }
    }

    /// <summary>
    /// Struct for storing information about files in the 7-zip archive.
    /// </summary>
    public struct ArchiveFileInfo
    {
        /// <summary>
        /// Gets or sets index of the file in the archive file table.
        /// </summary>
        [CLSCompliant(false)]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file last write time.
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        /// <summary>
        /// Gets or sets the file creation time.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the file last access time.
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// Gets or sets size of the file (unpacked).
        /// </summary>
        [CLSCompliant(false)]
        public ulong Size { get; set; }

        /// <summary>
        /// Gets or sets CRC checksum of the file.
        /// </summary>
        [CLSCompliant(false)]
        public uint Crc { get; set; }

        /// <summary>
        /// Gets or sets file attributes.
        /// </summary>
        [CLSCompliant(false)]
        public uint Attributes { get; set; }

        /// <summary>
        /// Gets or sets being a directory.
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Gets or sets being encrypted.
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        /// Gets or sets comment for the file.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Determines whether the specified System.Object is equal to the current ArchiveFileInfo.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return (obj is ArchiveFileInfo) ? Equals((ArchiveFileInfo)obj) : false;
        }

        /// <summary>
        /// Determines whether the specified ArchiveFileInfo is equal to the current ArchiveFileInfo.
        /// </summary>
        public bool Equals(ArchiveFileInfo afi)
        {
            return afi.Index == Index && afi.FileName == FileName;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        public override int GetHashCode()
        {
            return FileName.GetHashCode() ^ Index;
        }

        /// <summary>
        /// Returns a System.String that represents the current ArchiveFileInfo.
        /// </summary>
        public override string ToString()
        {
            return "[" + Index.ToString(CultureInfo.CurrentCulture) + "] " + FileName;
        }

        public static bool operator ==(ArchiveFileInfo afi1, ArchiveFileInfo afi2) => afi1.Equals(afi2);
        public static bool operator !=(ArchiveFileInfo afi1, ArchiveFileInfo afi2) => !afi1.Equals(afi2);
    }

    /// <summary>
    /// Archive property struct.
    /// </summary>
    public struct ArchiveProperty
    {
        /// <summary>
        /// Gets the name of the archive property.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the value of the archive property.
        /// </summary>
        public object Value { get; internal set; }

        /// <summary>
        /// Determines whether the specified System.Object is equal to the current ArchiveProperty.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return (obj is ArchiveProperty) ? Equals((ArchiveProperty)obj) : false;
        }

        /// <summary>
        /// Determines whether the specified ArchiveProperty is equal to the current ArchiveProperty.
        /// </summary>
        public bool Equals(ArchiveProperty afi)
        {
            return afi.Name == Name && afi.Value == Value;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Value.GetHashCode();
        }

        /// <summary>
        /// Returns a System.String that represents the current ArchiveProperty.
        /// </summary>
        public override string ToString()
        {
            return Name + " = " + Value;
        }

        public static bool operator ==(ArchiveProperty afi1, ArchiveProperty afi2) => afi1.Equals(afi2);
        public static bool operator !=(ArchiveProperty afi1, ArchiveProperty afi2) => !afi1.Equals(afi2);
    }

#if COMPRESS

    /// <summary>
    /// Archive compression mode.
    /// </summary>
    public enum CompressionMode
    {
        /// <summary>
        /// Create a new archive; overwrite the existing one.
        /// </summary>
        Create,
        /// <summary>
        /// Add data to the archive.
        /// </summary>
        Append,
    }

    internal enum InternalCompressionMode
    {
        /// <summary>
        /// Create a new archive; overwrite the existing one.
        /// </summary>
        Create,
        /// <summary>
        /// Add data to the archive.
        /// </summary>
        Append,
        /// <summary>
        /// Modify archive data.
        /// </summary>
        Modify
    }

    /// <summary>
    /// Zip encryption method enum.
    /// </summary>
    public enum ZipEncryptionMethod
    {
        /// <summary>
        /// ZipCrypto encryption method.
        /// </summary>
        ZipCrypto,
        /// <summary>
        /// AES 128 bit encryption method.
        /// </summary>
        Aes128,
        /// <summary>
        /// AES 192 bit encryption method.
        /// </summary>
        Aes192,
        /// <summary>
        /// AES 256 bit encryption method.
        /// </summary>
        Aes256
    }

    /// <summary>
    /// Archive update data for UpdateCallback.
    /// </summary>
    internal struct UpdateData
    {
        public uint FilesCount;
        public InternalCompressionMode Mode;
        public Dictionary<int, string>? FileNamesToModify { get; set; }
        public List<ArchiveFileInfo>? ArchiveFileData { get; set; }
    }
#endif
#endif
}
