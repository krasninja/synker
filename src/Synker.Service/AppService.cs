using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using Saritasa.Tools.Common.Extensions;
using Saritasa.Tools.Common.Utils;
using Synker.Common.Bundles;
using Synker.Common.ProfileLoaders;
using Synker.Core;
using Synker.UseCases.StartMonitor;
using Synker.UseCases.StopMonitor;
using Synker.Web;
using Topshelf;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using NullTarget = Synker.Common.Targets.NullTarget;

namespace Synker.Service
{
    /// <summary>
    /// Application service.
    /// </summary>
    public class AppService : ServiceControl
    {
        private const string ConfigFile = ".synker.config";
        private const string ProfilesSourceKey = "profiles-source";
        private const string BundlesDirectoryKey = "bundles-directory";
        private const string LogFileKey = "log-file";
        private const string DisableImportKey = "disable-import";
        private const string DisableExportKey = "disable-export";

        private static readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private IList<Profile> profiles = new List<Profile>();

        private IBundleFactory bundleFactory;

        private DelayActionRunner<Profile> delayActionRunner;

        #region ServiceControl

        /// <inheritdoc />
        public bool Start(HostControl hostControl)
        {
            var exitCode = StartAsync(hostControl).GetAwaiter().GetResult();
            return exitCode == 0;
        }

        private async Task<int> StartAsync(HostControl hostControl)
        {
            // Read configuration.
            var configFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ConfigFile);
            if (!File.Exists(configFile))
            {
                Console.Error.WriteLine($"Cannot find config file ${configFile}.");
                return 1;
            }
            var configData = GetConfigData(configFile);

            // Setup logging.
            AppLogger.LoggerFactory = configData.ContainsKey(LogFileKey) ?
                ConfigureFileLogging(configData[LogFileKey]) :
                ConfigureConsoleLogging();

            if (!configData.ContainsKey(ProfilesSourceKey))
            {
                Console.Error.WriteLine($"Cannot find property {ProfilesSourceKey}.");
                return 2;
            }
            if (!configData.ContainsKey(BundlesDirectoryKey))
            {
                Console.Error.WriteLine($"Cannot find property {BundlesDirectoryKey}.");
                return 3;
            }

            // Setup profiles and start monitoring.
            ProfileFactory.AddTargetTypesFromAssembly(typeof(NullTarget).Assembly);
            var filesProfileLoader = new FilesProfileLoader(configData[ProfilesSourceKey]);
            bundleFactory = new ZipBundleFactory(configData[BundlesDirectoryKey]);
            profiles = await ProfileFactory.LoadAsync(filesProfileLoader);
            var startMonitorCommand = new StartMonitorCommand(profiles, bundleFactory)
            {
                DisableExport = !StringUtils.ParseOrDefault(
                    configData.GetValueOrDefault(UserConfiguration.DisableExportKey), true),
                DisableImport = !StringUtils.ParseOrDefault(
                    configData.GetValueOrDefault(UserConfiguration.DisableImportKey), true)
            };
            delayActionRunner = await startMonitorCommand.ExecuteAsync();

            // Start web service.
            await Server.Create(bundleFactory, profiles).RunAsync();

            return 0;
        }

        /// <inheritdoc />
        public bool Stop(HostControl hostControl)
        {
            new StopMonitorCommand(profiles, bundleFactory).ExecuteAsync().GetAwaiter().GetResult();
            delayActionRunner.Dispose();

            return true;
        }

        #endregion

        private static IDictionary<string, string> GetConfigData(string file)
        {
            using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
            using var sr = new StreamReader(fileStream);
            var profile = deserializer.Deserialize<IDictionary<string, string>>(sr.ReadToEnd());
            return profile;
        }

        private const string NLogLayout =
            @"${date:format=yyyy-MM-dd HH\:mm\:ss} [${level:format=FirstCharacter}] ${logger:shortName=true}: ${message} ${exception}";

        private static ILoggerFactory ConfigureConsoleLogging()
        {
            // Config NLog.
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget("console")
            {
                Layout = NLogLayout
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget);
            LogManager.Configuration = config;

            return CreateLoggerFactory();
        }

        private static ILoggerFactory ConfigureFileLogging(string fileName)
        {
            // Config NLog.
            var config = new LoggingConfiguration();
            var consoleTarget = new FileTarget("file")
            {
                FileName = fileName,
                Layout = NLogLayout
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget);
            LogManager.Configuration = config;

            return CreateLoggerFactory();
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            // Setup integration with Extensions.Logging .
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddNLog(new NLogProviderOptions
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true
                    });
                })
                .BuildServiceProvider();
            return serviceProvider.GetRequiredService<ILoggerFactory>();
        }
    }
}