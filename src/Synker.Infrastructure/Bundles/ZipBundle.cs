using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Synker.Domain;

namespace Synker.Infrastructure.Bundles
{
    /// <summary>
    /// Bundle that uses ZIP streams.
    /// </summary>
    public class ZipBundle : IBundle, IDisposable
    {
        public const string CreationTimeFormat = "yyyyMMdd-HHmmss";

        private const int LatestFormatVersion = 1;
        private const string Key_FormatVersion = "format-version";

        private readonly ZipOutputStream zipOutputStream;
        private readonly ZipFile zipFile;
        private readonly string id;

        private int groupIndex = 0;
        private readonly byte[] buffer = new byte[4096];

        /// <inheritdoc />
        public string Id => id;

        /// <summary>
        /// Constructor for write mode.
        /// </summary>
        /// <param name="profileId">Profile identifier.</param>
        /// <param name="lastUpdateDate">Last update date of all settings.</param>
        /// <param name="directory">Output directory.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ZipBundle(string profileId, DateTime lastUpdateDate, string directory)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                throw new ArgumentNullException(nameof(profileId));
            }
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentNullException(nameof(directory));
            }
            this.id = $"{profileId}@{lastUpdateDate.ToUniversalTime().ToString(CreationTimeFormat)}";
            this.zipOutputStream = new ZipOutputStream(new FileStream(Path.Combine(directory, $"{id}.zip"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite));
            this.zipOutputStream.SetLevel(Deflater.BEST_COMPRESSION);
        }

        /// <summary>
        /// Constructor for read mode.
        /// </summary>
        /// <param name="zipFile">ZIP file.</param>
        /// <param name="id">Bundle id.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ZipBundle(ZipFile zipFile, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            this.zipFile = zipFile ?? throw new ArgumentNullException(nameof(zipFile));
            this.id = id;
        }

        /// <inheritdoc />
        public Task<string> PutSettingAsync(Setting setting, string targetId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (zipOutputStream == null)
            {
                throw new BundleException($"{nameof(ZipBundle)} is not opened in write mode.");
            }
            var entityId = FormatEntityName(setting.Id, targetId);
            var entry = new ZipEntry(entityId);
            zipOutputStream.PutNextEntry(entry);
            StreamUtils.Copy(setting.Stream, zipOutputStream, buffer);
            setting.Stream.Close();
            zipOutputStream.CloseEntry();

            foreach (var metadataItem in setting.Metadata)
            {
                cancellationToken.ThrowIfCancellationRequested();

                PutStringEntryToZip(zipOutputStream,
                    FormatEntityName($"{setting.Id}.{metadataItem.Key}", targetId), metadataItem.Value);
            }
            return Task.FromResult(entityId);
        }

        /// <inheritdoc />
        public Task PutMetadataAsync(IDictionary<string, string> metadata, string targetId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (zipOutputStream == null)
            {
                throw new BundleException($"{nameof(ZipBundle)} is not opened in write mode.");
            }
            foreach (var keyValuePair in metadata)
            {
                cancellationToken.ThrowIfCancellationRequested();

                PutStringEntryToZip(
                    zipOutputStream,
                    FormatMetadataEntityName(keyValuePair.Key, targetId),
                    keyValuePair.Value);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Setting> GetSettingsAsync(string targetId)
        {
            if (zipFile == null)
            {
                throw new BundleException($"{nameof(ZipBundle)} is not opened in read mode.");
            }

            // Open it and prepare list of entries.
            var entries = new List<ZipEntry>();
            foreach (ZipEntry entry in zipFile)
            {
                entries.Add(entry);
            }

            // For every target directory get entries.
            foreach (var zipTargetEntries in entries
                .Where(e => IsTargetEntry(e.Name))
                .GroupBy(e => GetTargetFromName(e.Name)))
            {
                foreach (var zipTargetProps in zipTargetEntries.GroupBy(e => GetPropIndexFromName(e.Name)))
                {
                    // Format setting.
                    var setting = new Setting();
                    foreach (var targetProp in zipTargetProps)
                    {
                        var name = GetPropFromName(targetProp.Name);
                        if (string.IsNullOrEmpty(name))
                        {
                            setting.Stream = zipFile.GetInputStream(targetProp);
                        }
                        else
                        {
                            setting.Metadata[name] = GetStringFromZipEntry(zipFile, targetProp.Name);
                        }
                    }

                    if (setting.Stream == null)
                    {
                        throw new SettingsSyncException("Target has no root entry.");
                    }

                    yield return setting;
                }
            }
        }

        /// <inheritdoc />
        public Task<IDictionary<string, string>> GetMetadataAsync(string targetId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (zipFile == null)
            {
                throw new BundleException($"{nameof(ZipBundle)} is not opened in read mode.");
            }

            var metadata = new Dictionary<string, string>();
            foreach (ZipEntry entry in zipFile)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.Name.Contains('/') ||
                    !entry.Name.StartsWith(targetId + "."))
                {
                    continue;
                }

                var key = entry.Name.Substring(entry.Name.IndexOf('.') + 1);
                metadata[key] = GetStringFromZipEntry(zipFile, entry.Name);
            }
            return Task.FromResult<IDictionary<string, string>>(metadata);
        }

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (zipOutputStream != null)
                {
                    PutMetadataAsync(new Dictionary<string, string>
                    {
                        [Key_FormatVersion] = LatestFormatVersion.ToString()
                    }, string.Empty);
                    zipOutputStream.Finish();
                    zipOutputStream.Flush();
                    zipOutputStream.Close();
                    zipOutputStream.Dispose();
                }
                ((IDisposable) zipFile)?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Formatting

        private string FormatEntityName(string name, string targetId) => $@"{targetId}/{groupIndex:000}/{name}";

        private string FormatMetadataEntityName(string name, string targetId = null) =>
            string.IsNullOrEmpty(targetId) ? "." + name : $@"{targetId}.{name}";

        #endregion

        #region ZIP helpers

        private void PutStringEntryToZip(ZipOutputStream zipStream, string entryName, string str)
        {
            zipStream.PutNextEntry(new ZipEntry(entryName));
            using (MemoryStream ms = new MemoryStream())
            {
                using TextWriter textWriter = new StreamWriter(ms);
                textWriter.Write(str);
                textWriter.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                StreamUtils.Copy(ms, zipStream, buffer);
            }
            zipStream.CloseEntry();
        }

        private static string GetStringFromZipEntry(ZipFile zipFile, string entryName)
        {
            var entry = zipFile.GetEntry(entryName);
            if (entry == null)
            {
                throw new InvalidOperationException($"{entryName} not found.");
            }

            using var sr = new StreamReader(zipFile.GetInputStream(entry));
            return sr.ReadToEnd();
        }

        #endregion

        #region Get data from entry name

        /// <summary>
        /// Target entry is always within its directory. So it has "/".
        /// </summary>
        /// <param name="str">Entry full name.</param>
        /// <returns>True if target entry.</returns>
        private static bool IsTargetEntry(string str) => str.Contains("/");

        /// <summary>
        /// Gets from "0/0.name" target value "0".
        /// </summary>
        /// <param name="str">Target string.</param>
        /// <returns>Target name (index).</returns>
        private static string GetTargetFromName(string str)
        {
            int ind = str.LastIndexOf("/", StringComparison.Ordinal);
            return ind > -1 ? str.Substring(0, ind) : str;
        }

        /// <summary>
        /// Return property name from entry name. "main/000/000.name" -> "name".
        /// </summary>
        /// <param name="str">Target entry name.</param>
        /// <returns>Property name or empty string.</returns>
        private static string GetPropFromName(string str)
        {
            var ind1 = str.LastIndexOf("/", StringComparison.Ordinal);
            if (ind1 < 0)
            {
                return string.Empty;
            }

            var ind2 = str.IndexOf(".", ind1, StringComparison.Ordinal);
            if (ind2 < 0)
            {
                return string.Empty;
            }

            return str.Substring(ind2 + 1);
        }

        /// <summary>
        /// Get property index from name. "000/000/000.name" -> "name".
        /// </summary>
        private static string GetPropIndexFromName(string str)
        {
            var ind1 = str.IndexOf("/", StringComparison.Ordinal);
            if (ind1 == -1)
            {
                return str;
            }

            var ind2 = str.LastIndexOf(".", StringComparison.Ordinal);
            if (ind2 == -1)
            {
                return str.Substring(ind1);
            }

            return str.Substring(ind1, ind2 - ind1);
        }

        #endregion
    }
}
