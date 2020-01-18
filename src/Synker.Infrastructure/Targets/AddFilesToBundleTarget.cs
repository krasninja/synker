using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.Extensions.Logging;
using Synker.Domain;

namespace Synker.Infrastructure.Targets
{
    /// <summary>
    /// Target processes files and directories.
    /// </summary>
    public class AddFilesToBundleTarget : TargetBase, ITargetWithMonitor, IDisposable
    {
        private const string Key_LastUpdate = "last-update";
        private const string Key_Name = "name";

        /// <summary>
        /// Base fullName to settings, may be different on different operation systems.
        /// </summary>
        [Required]
        public string BasePath { get; set; } = string.Empty;

        /// <summary>
        /// Files or directories to export or import.
        /// </summary>
        [Required]
        public IList<string> Files { get; } = new List<string>();

        /// <summary>
        /// Skip file export if it does not exist on target system.
        /// </summary>
        public bool SkipIfNotExists { get; set; }

        /// <summary>
        /// Regexp patterns for files exclude.
        /// </summary>
        /// <remarks>Service https://regex101.com/ can be used for better testing.</remarks>
        public IList<string> ExcludePatterns { get; set; } = new List<string>();

        private static readonly ILogger<AddFilesToBundleTarget> logger = AppLogger.Create<AddFilesToBundleTarget>();

        private FileSystemWatcher watcher;

        public AddFilesToBundleTarget()
        {
            if (!string.IsNullOrEmpty(BasePath) && !Path.IsPathRooted(BasePath))
            {
                throw new SettingsSyncException($"Base fullName \"{BasePath}\" is not rooted.");
            }
        }

        /// <inheritdoc />
        public override async IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext)
        {
            // Export file by file.
            foreach (var file in GetAllFiles())
            {
                // Does file exists.
                var result = new Setting();
                if (!File.Exists(file))
                {
                    if (SkipIfNotExists)
                    {
                        continue;
                    }

                    throw new TargetException($"Cannot find file {file}.");
                }

                // Export.
                result.Stream = File.OpenRead(file);
                result.Metadata[Key_Name] = NormalizePath(
                    GetRelativeFilePath(BasePath, file, IsFileSystemCaseInsensitive())
                );
                result.Metadata[Key_LastUpdate] = File.GetLastWriteTimeUtc(file).Ticks.ToString();
                logger.LogInformation("Export file {fileName}.", result.Metadata[Key_Name]);
                yield return result;
            }
        }

        /// <remarks>
        /// Source: https://stackoverflow.com/questions/51179331/is-it-possible-to-use-fullName-getrelativepath-net-core2-in-winforms-proj-targeti .
        /// </remarks>
        internal static string GetRelativeFilePath(string relativeTo, string fullName, bool caseInsensitive)
        {
            if (relativeTo.Length < 1)
            {
                throw new ArgumentException("Incorrect relative path.");
            }
            if (fullName.Length < 1 || fullName.Length <= relativeTo.Length)
            {
                throw new ArgumentException("Incorrect full file name.");
            }

            int startRelativeIndex = -1;
            char separator = fullName.Contains('\\') ? '\\' : '/';
            for (int i = 0; i < relativeTo.Length; i++)
            {
                if (fullName.Length <= i)
                {
                    throw new SettingsSyncException(
                        $"Incorrect full file name {fullName} for relative path {relativeTo}.");
                }

                if (relativeTo[i] == '\\' || relativeTo[i] == '/')
                {
                     continue;
                }

                if ((caseInsensitive && char.ToUpper(relativeTo[i]) != char.ToUpper(fullName[i])) ||
                     (!caseInsensitive && relativeTo[i] != fullName[i]))
                {
                    if (i > 0 && (relativeTo[i - 1] == '\\' || relativeTo[i - 1] == '/'))
                    {
                        startRelativeIndex = i;
                        break;
                    }
                    throw new SettingsSyncException($"Incorrect relative path {relativeTo}.");
                }
            }

            if (relativeTo.EndsWith("\\") || relativeTo.EndsWith("/"))
            {
                if (fullName[relativeTo.Length - 1] == '\\' || fullName[relativeTo.Length - 1] == '/')
                {
                    startRelativeIndex = relativeTo.Length;
                }
            }
            else
            {
                if (fullName[relativeTo.Length] == '\\' || fullName[relativeTo.Length] == '/')
                {
                    startRelativeIndex = relativeTo.Length;
                }
            }

            if (startRelativeIndex < 0)
            {
                throw new SettingsSyncException(
                    $"Cannot get relative file name for {fullName} and relative path {relativeTo}.");
            }

            var relativeFilePath = fullName.Substring(startRelativeIndex);
            if (relativeFilePath.StartsWith("/") || relativeFilePath.StartsWith("\\"))
            {
                relativeFilePath = "." + relativeFilePath;
            }
            else
            {
                relativeFilePath = "." + separator + relativeFilePath;
            }
            return relativeFilePath;
        }

        // Source: https://github.com/gapotchenko/Gapotchenko.FX/blob/master/Source/Gapotchenko.FX.IO/FileSystem.cs#L51
        private static bool IsFileSystemCaseInsensitive()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))  // HFS+ (the Mac file-system) is usually configured to be case insensitive.
            {
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return false;
            }
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return false;
            }
            // A sane default.
            return true;
        }

        /// <inheritdoc />
        public override async Task ImportAsync(
            SyncContext syncContext,
            IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[] buffer = new byte[4096];
            await foreach(var setting in settings.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.Combine(Path.GetFullPath(BasePath), setting.Metadata[Key_Name]);
                logger.LogInformation("Update file {fileName}.", fileName);
                var fileDirectory = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(fileDirectory))
                {
                    Directory.CreateDirectory(fileDirectory);
                }
                using (var file = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    file.SetLength(0);
                    StreamUtils.Copy(setting.Stream, file, buffer);
                }
                var ticks = long.Parse(setting.Metadata[Key_LastUpdate]);
                var lastBundleUpdateDate = new DateTime(ticks, DateTimeKind.Utc);
                File.SetLastWriteTime(fileName, lastBundleUpdateDate.ToLocalTime());
            };
        }

        /// <inheritdoc />
        public override Task<DateTime?> GetLastUpdateDateTimeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateTime? dateTime = null;
            string fileWithLatestDate = string.Empty;
            foreach (var file in GetAllFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileLastUpdate = File.GetLastWriteTimeUtc(file);
                if (!dateTime.HasValue || fileLastUpdate > dateTime)
                {
                    fileWithLatestDate = file;
                    dateTime = fileLastUpdate;
                }
            }

            if (dateTime.HasValue)
            {
                logger.LogDebug($"Found last update date {dateTime} of file {fileWithLatestDate}.");
            }
            return Task.FromResult(dateTime);
        }

        private IEnumerable<string> GetAllFiles()
        {
            var excludePatternsRegex = GetExcludePatterns();

            foreach (string file in this.Files)
            {
                var resolvedPath = file;

                // Correct relative files.
                if (file.StartsWith("~/"))
                {
                    if (string.IsNullOrEmpty(BasePath))
                    {
                        throw new SettingsSyncException($"Relative file {file} specified but no base fullName.");
                    }
                    resolvedPath = Path.Combine(BasePath, file.Substring(2));
                }

                // If rooted file - just return.
                if (File.Exists(resolvedPath) && !MatchAnyRegexInList(excludePatternsRegex, resolvedPath))
                {
                    yield return resolvedPath;
                }

                // Try to enumerate from directory.
                var path = HasWildcardCharacters(resolvedPath) ? Path.GetDirectoryName(resolvedPath) : resolvedPath;
                var pattern = HasWildcardCharacters(resolvedPath) ? Path.GetFileName(resolvedPath) : "*";
                if (!Directory.Exists(path))
                {
                    continue;
                }

                var dirFiles = Enumerable.Empty<string>();
                try
                {
                    dirFiles = Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly);
                }
                catch (DirectoryNotFoundException)
                {
                    logger.LogWarning($"Cannot enumerate files for {BasePath} and {file}.");
                }
                foreach (var dirFile in dirFiles)
                {
                    if (!MatchAnyRegexInList(excludePatternsRegex, dirFile))
                    {
                        yield return dirFile;
                    }
                }
            }
        }

        private static bool HasWildcardCharacters(string path)
        {
            var fileName = Path.GetFileName(path);
            return fileName.Contains('*') || fileName.Contains('?');
        }

        private bool MatchAnyRegexInList(IList<Regex> regexList, string file)
        {
            var normalizedName = NormalizePath(file);
            foreach (Regex regex in regexList)
            {
                if (regex.IsMatch(normalizedName))
                {
                    logger.LogTrace($"File {normalizedName} match pattern {regex}.");
                    return true;
                }
            }
            return false;
        }

        private IList<Regex> GetExcludePatterns()
        {
            var excludeRegexps = new List<Regex>();
            foreach (var excludePattern in this.ExcludePatterns)
            {
                excludeRegexps.Add(new Regex(excludePattern));
            }
            return excludeRegexps;
        }

        private static string NormalizePath(string path) => path.Replace('\\', '/');

        #region ITargetWithMonitor

        /// <inheritdoc />
        public event EventHandler<ITarget> OnSettingsUpdate;

        /// <inheritdoc />
        public void StartMonitor()
        {
            if (watcher != null)
            {
                logger.LogInformation("Files watcher is already running.");
                return;
            }
            allSyncFiles = new HashSet<string>(GetAllFiles());
            logger.LogInformation("Start files watcher.");
            watcher = new FileSystemWatcher(BasePath);
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Created += WatcherEvent;
            watcher.Changed += WatcherEvent;
            watcher.Deleted += WatcherEvent;
            watcher.Renamed += WatcherEvent;
            watcher.EnableRaisingEvents = true;
        }

        /// <inheritdoc />
        public void StopMonitor()
        {
            if (watcher != null)
            {
                logger.LogInformation("Stop files watcher.");
                watcher.EnableRaisingEvents = false;
                watcher.Created -= WatcherEvent;
                watcher.Changed -= WatcherEvent;
                watcher.Deleted -= WatcherEvent;
                watcher.Renamed -= WatcherEvent;
                watcher.Dispose();
                watcher = null;
            }
        }

        private HashSet<string> allSyncFiles = new HashSet<string>();

        private void WatcherEvent(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType.HasFlag(WatcherChangeTypes.Created))
            {
                allSyncFiles = new HashSet<string>(GetAllFiles());
            }

            if (allSyncFiles.Contains(e.FullPath))
            {
                logger.LogTrace($"Invoke WatcherEvent because of file {e.FullPath}.");
                OnSettingsUpdate?.Invoke(this, this);
            }
        }

        #endregion

        #region IDisposable

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    StopMonitor();
                }
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
