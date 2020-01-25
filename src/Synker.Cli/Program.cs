using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using Synker.Cli.Commands;
using Synker.Domain;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using NullTarget = Synker.Infrastructure.Targets.NullTarget;

namespace Synker.Cli
{
    /// <summary>
    /// Entry point class.
    /// </summary>
    [Command(Name = "synker-cli", Description = "Applications settings synchronization utility.",
        ThrowOnUnexpectedArgument = true)]
    [Subcommand(typeof(Clean))]
    [Subcommand(typeof(Export))]
    [Subcommand(typeof(Import))]
    [Subcommand(typeof(Sync))]
    internal class Program
    {
        private static ILogger<Program> logger = new NullLogger<Program>();

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Startup arguments.</param>
        /// <returns>Exit code.</returns>
        public static async Task<int> Main(string[] args)
        {
            // Setup unhandled exceptions handler.
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is SettingsSyncException settingsSyncException)
                {
                    logger.LogError(settingsSyncException.Message);
                }
                else if (e.ExceptionObject is Exception exception)
                {
                    logger.LogCritical(exception.Message);
                }
                else
                {
                    logger.LogCritical(e.ExceptionObject.ToString());
                }
            };

            // Logging.
            AppLogger.LoggerFactory = ConfigureLogging();
            logger = AppLogger.Create<Program>();
            logger.LogInformation($"Application startup at {DateTime.Now:yyyy-MM-dd}.");
            ProfileFactory.AddTargetTypesFromAssembly(typeof(NullTarget).Assembly);

            return await CommandLineApplication.ExecuteAsync<Program>(args);
        }

        private Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            app.ShowHelp(usePager: false);
            return Task.FromResult(1);
        }

        private static ILoggerFactory ConfigureLogging()
        {
            // Config NLog.
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget("console")
            {
                Layout =
                    @"${date:format=HH\:mm\:ss} [${level:format=FirstCharacter}] ${logger:shortName=true}: ${message} ${exception}"
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget);
            LogManager.Configuration = config;

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
