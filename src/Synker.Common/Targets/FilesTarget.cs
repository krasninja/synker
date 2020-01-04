using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.Extensions.Logging;
using Synker.Core;

namespace Synker.Common.Targets
{
    /// <summary>
    /// Target processes files and directories.
    /// </summary>
    public class FilesTarget : TargetBase, ITargetWithMonitor, IDisposable
    {
        private const string Key_LastUpdate = "last-update";
        private const string Key_Name = "name";

        /// <summary>
        /// Base path to settings, may be different on different operation systems.
        /// </summary>
        [Required]
        public string BasePath { get; set; } = string.Empty;

        /// <summary>
        /// Files or directories to export or import.
        /// </summary>
        [Required]
        public IList<string> Files { get; set; } = new List<string>();

        /// <summary>
        /// Skip file export if it does not exist on target system.
        /// </summary>
        public bool SkipIfNotExists { get; set; }

        /// <summary>
        /// Regexp patterns for files exclude.
        /// </summary>
        /// <remarks>Service https://regex101.com/ can be used for better testing.</remarks>
        public IList<string> ExcludePatterns { get; set; } = new List<string>();

        private static readonly ILogger<FilesTarget> logger = AppLogger.Create<FilesTarget>();

        private FileSystemWatcher watcher;

        public FilesTarget()
        {
            if (!string.IsNullOrEmpty(BasePath) && !Path.IsPathRooted(BasePath))
            {
                throw new SettingsSyncException($"Base path \"{BasePath}\" is not rooted.");
            }
        }

        /// <inheritdoc />
        public override IAsyncEnumerable<Setting> ExportAsync(
            SyncContext syncContext,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return new AsyncEnumerable<Setting>(async yield =>
            {
                // Export file by file.
                foreach (var file in GetAllFiles())
                {
                    yield.CancellationToken.ThrowIfCancellationRequested();

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
                    result.Metadata[Key_Name] = NormalizeFileName(
                        GetRelativePath(BasePath, file)
                    );
                    result.Metadata[Key_LastUpdate] = File.GetLastWriteTimeUtc(file).Ticks.ToString();
                    logger.LogInformation("Export file {fileName}.", result.Metadata[Key_Name]);
                    await yield.ReturnAsync(result);
                }
            });
        }

        /// <remarks>
        /// Source: https://stackoverflow.com/questions/51179331/is-it-possible-to-use-path-getrelativepath-net-core2-in-winforms-proj-targeti .
        /// </remarks>
        private static string GetRelativePath(string relativeTo, string path)
        {
            var uri = new Uri(relativeTo);
            var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString())
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{ Path.DirectorySeparatorChar }{ rel }";
            }
            return rel;
        }

        /// <inheritdoc />
        public override async Task ImportAsync(
            SyncContext syncContext,
            IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[] buffer = new byte[4096];
            await settings.ForEachAsync(setting =>
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
            }, cancellationToken: cancellationToken);
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
                        throw new SettingsSyncException($"Relative file {file} specified but no base path.");
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
            var normalizedName = NormalizeFileName(file);
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

        private static string NormalizeFileName(string file) => file.Replace('\\', '/');

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
        private DateTime lastSettingsUpdate = DateTime.Now;

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
