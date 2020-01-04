using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Synker.Core
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

        private UserConfiguration()
        {
        }

        /// <summary>
        /// Read and return user configuration as key-value map.
        /// </summary>
        /// <param name="configFile">Config file or default one.</param>
        /// <returns>Configuration.</returns>
        /// <exception cref="InvalidOperationException">File not found.</exception>
        public static IDictionary<string, string> Get(string configFile = null)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                configFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ConfigFile);
            }
            if (!File.Exists(configFile))
            {
                throw new InvalidOperationException($"Cannot find config file ${configFile}.");
            }
            return GetConfigData(configFile);
        }

        private static IDictionary<string, string> GetConfigData(string file)
        {
            using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
            using var sr = new StreamReader(fileStream);
            var profile = deserializer.Deserialize<IDictionary<string, string>>(sr.ReadToEnd());
            return profile;
        }
    }
}
