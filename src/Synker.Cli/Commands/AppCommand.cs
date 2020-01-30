using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using Saritasa.Tools.Domain.Exceptions;
using Synker.Domain;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using NullTarget = Synker.Infrastructure.Targets.NullTarget;

namespace Synker.Cli.Commands
{
    /// <summary>
    /// Application command.
    /// </summary>
    internal class AppCommand
    {
        [Option("-l|--log-level", "Minimum log level.", CommandOptionType.SingleValue)]
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        protected virtual Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            // Logging.
            AppLogger.LoggerFactory = ConfigureLogging();
            var logger = AppLogger.Create<Program>();
            logger.LogTrace($"Application startup at {DateTime.Now:yyyy-MM-dd}.");
            ProfileFactory.AddTargetTypesFromAssembly(typeof(NullTarget).Assembly);

            ValidationException.MessageFormatter = ValidationExceptionDelegates.GroupErrorsOrDefaultMessageFormatter;
            return Task.FromResult(0);
        }

        private ILoggerFactory ConfigureLogging()
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
                    builder.SetMinimumLevel(LogLevel);
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
