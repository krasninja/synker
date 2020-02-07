using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Saritasa.Tools.Domain.Exceptions;
using Synker.Cli.Commands;
using Synker.Domain;
using Monitor = Synker.Cli.Commands.Monitor;

namespace Synker.Cli
{
    /// <summary>
    /// Entry point class.
    /// </summary>
    [Command(Name = "synker-cli", Description = "Applications settings synchronization utility.",
        ThrowOnUnexpectedArgument = true)]
    [VersionOptionFromMember("-v|--version", MemberName = nameof(GetVersion))]
    [Subcommand(
        typeof(Clean),
        typeof(Export),
        typeof(Import),
        typeof(Sync),
        typeof(Monitor))]
    internal class Program : AppCommand
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Startup arguments.</param>
        /// <returns>Exit code.</returns>
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var app = new CommandLineApplication<Program>
                {
                    UsePagerForHelpText = false
                };
                app.Conventions.UseDefaultConventions();
                return await app.ExecuteAsync(args);
            }
            catch (DomainException domainException)
            {
                var logger = AppLogger.Create<Program>();
                logger.LogError(domainException.Message);
                return -2;
            }
        }

        protected override Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            app.ShowHelp();
            return Task.FromResult(1);
        }

        private static string GetVersion()
            => typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
