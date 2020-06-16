using System;
using System.Collections.Generic;
using System.IO;
using Saritasa.Tools.Common.Extensions;
using Saritasa.Tools.Common.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Synker.Domain
{
    /// <summary>
    /// User configuration that is read from config file or other source.
    /// </summary>
    public class UserConfiguration
    {
        public const string ConfigFile = ".synker.config";
        public const string ProfilesSourceKey = "profiles-source";
        public const string BundlesDirectoryKey = "bundles-directory";
        public const string LogFileKey = "log-file";
        public const string DisableImportKey = "disable-import";
        public const string DisableExportKey = "disable-export";

        private static readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private readonly IDictionary<string, string> data = new Dictionary<string, string>();

        /// <summary>
        /// The file name which from the config was loaded.
        /// </summary>
        public string FileName { get; private set; }

        private UserConfiguration()
        {
        }

        private UserConfiguration(IDictionary<string, string> data)
        {
            this.data = data;
        }

        public string ProfilesSource => GetOrThrow(ProfilesSourceKey);

        public string BundlesDirectory => GetOrThrow(BundlesDirectoryKey);

        public string LogFile => DictionaryUtils.GetValueOrDefault(data, LogFileKey, string.Empty);

        public bool DisableImport => StringUtils.ParseOrDefault(
            DictionaryUtils.GetValueOrDefault(data, DisableImportKey), false);

        public bool DisableExport => StringUtils.ParseOrDefault(
            DictionaryUtils.GetValueOrDefault(data, DisableExportKey), false);

        /// <summary>
        /// Returns config value by key or throw exception.
        /// </summary>
        /// <param name="key">Dictionary key.</param>
        /// <returns>Value returned by key.</returns>
        /// <exception cref="SettingsSyncException">Key doesn't exist.</exception>
        private string GetOrThrow(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (!data.ContainsKey(key))
            {
                throw new SettingsSyncException($"Cannot find key {key}.");
            }
            return data[key];
        }

        /// <summary>
        /// Apply default values based on provided user options.
        /// </summary>
        public void ApplyDefaults()
        {
            // By default have bundles in config file's directory.
            if (string.IsNullOrEmpty(data.GetValueOrDefault(BundlesDirectoryKey, string.Empty)) &&
                !string.IsNullOrEmpty(FileName))
            {
                data[BundlesDirectoryKey] = Path.GetDirectoryName(FileName);
            }
            if (string.IsNullOrEmpty(data.GetValueOrDefault(BundlesDirectoryKey, string.Empty)) &&
                !string.IsNullOrEmpty(data.GetValueOrDefault(ProfilesSourceKey, string.Empty)))
            {
                data[BundlesDirectoryKey] = Path.GetDirectoryName(ProfilesSource);
            }
        }

        /// <summary>
        /// Read and return user configuration as key-value map.
        /// </summary>
        /// <param name="configFile">Config file or default one.</param>
        /// <returns>Configuration.</returns>
        /// <exception cref="InvalidOperationException">File not found.</exception>
        public static UserConfiguration LoadFromFile(string configFile = null)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                configFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ConfigFile);
            }
            if (!File.Exists(configFile))
            {
                throw new SettingsSyncException($"Cannot find default config file {configFile}.");
            }
            return GetConfigData(configFile);
        }

        private static UserConfiguration GetConfigData(string file)
        {
            using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fileStream);
            var data = deserializer.Deserialize<IDictionary<string, string>>(sr.ReadToEnd());
            return new UserConfiguration(data)
            {
                FileName = file
            };
        }

        /// <summary>
        /// Create empty configuration.
        /// </summary>
        /// <returns>Empty configuration.</returns>
        public static UserConfiguration CreateEmpty(Action<IDictionary<string, string>> setup)
        {
            var userConfiguration = new UserConfiguration();
            setup(userConfiguration.data);
            return userConfiguration;
        }
    }
}
