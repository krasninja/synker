using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Synker.Domain;

namespace Synker.Infrastructure.Bundles
{
    /// <summary>
    /// Bundle factory to handle ZIP bundles.
    /// </summary>
    public class ZipBundleFactory : IBundleFactory, IBundleFactoryWithMonitor
    {
        private const string FileExtension = ".zip";

        private readonly string directory;

        private FileSystemWatcher watcher;

        private static readonly ILogger<Profile> logger = AppLogger.Create<Profile>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="directory">Directory with all bundles.</param>
        public ZipBundleFactory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentNullException(nameof(directory));
            }
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            this.directory = Path.GetFullPath(directory);
        }

        #region IBundleFactory

        /// <inheritdoc />
        public Task<IReadOnlyList<BundleInfo>> GetAllAsync(string profileId, CancellationToken cancellationToken =
            default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new DirectoryInfo(directory)
                .GetFiles($"{profileId}@*-*{FileExtension}", SearchOption.TopDirectoryOnly)
                .Where(f => string.Compare(Path.GetFileName(GetProfileFromName(f.Name)),
                                profileId, StringComparison.OrdinalIgnoreCase) == 0)
                .OrderBy(f => f.Name)
                .Select(file =>
                    new BundleInfo(
                        Path.GetFileNameWithoutExtension(file.Name),
                        GetCreationTimeFromName(Path.GetFileNameWithoutExtension(file.Name)),
                        file.Length
                    )
                )
                .ToList();
            return Task.FromResult<IReadOnlyList<BundleInfo>>(result);
        }

        /// <inheritdoc />
        public Task<IBundle> OpenAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bundle =
                new ZipBundle(
                    new ZipFile(Path.Combine(directory, id + FileExtension)),
                    id);
            return Task.FromResult<IBundle>(bundle);
        }

        /// <inheritdoc />
        public Task<IBundle> CreateAsync(string profileId, DateTime lastUpdateDate, CancellationToken cancellationToken =
            default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IBundle>(new ZipBundle(profileId, lastUpdateDate, directory));
        }

        /// <inheritdoc />
        public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation($"Remove bundle {id}.");
            File.Delete(Path.Combine(directory, id + FileExtension));
            return Task.CompletedTask;
        }

        #endregion

        #region IBundleFactoryWithMonitor

        /// <inheritdoc />
        public event EventHandler<string> OnSettingsUpdate;

        /// <inheritdoc />
        public void StartMonitor()
        {
            if (watcher != null)
            {
                logger.LogInformation("Watcher is already running.");
                return;
            }
            logger.LogInformation("Start watcher.");
            watcher = new FileSystemWatcher(directory);
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName;
            watcher.Created += WatcherEvent;
            watcher.Changed += WatcherEvent;
            watcher.EnableRaisingEvents = true;
        }

        /// <inheritdoc />
        public void StopMonitor()
        {
            if (watcher != null)
            {
                logger.LogInformation("Stop watcher.");
                watcher.EnableRaisingEvents = false;
                watcher.Created -= WatcherEvent;
                watcher.Changed -= WatcherEvent;
                watcher.Dispose();
                watcher = null;
            }
        }

        private void WatcherEvent(object sender, FileSystemEventArgs e)
        {
            var profile = GetProfileFromName(e.Name);
            logger.LogTrace($"Invoke WatcherEvent because of file {e.FullPath}.");
            OnSettingsUpdate?.Invoke(this, profile);
        }

        #endregion

        /// <summary>
        /// Get profile name from file name. For example "jetbrains-rider@20190106-040642.zip" -> "jetbrains-rider".
        /// </summary>
        /// <param name="name">File name.</param>
        /// <returns>Profile name or empty.</returns>
        private static string GetProfileFromName(string name)
        {
            var ind = name.IndexOf('@');
            return ind > -1 && name.Contains("-") && Path.HasExtension(FileExtension)
                ? name.Substring(0, ind) : string.Empty;
        }

        private static DateTime GetCreationTimeFromName(string name)
        {
            var ind = name.IndexOf('@');
            return DateTime.ParseExact(name.Substring(ind + 1),
                ZipBundle.CreationTimeFormat, CultureInfo.InvariantCulture);
        }
    }
}
