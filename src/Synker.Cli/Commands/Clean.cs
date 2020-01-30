using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Infrastructure.Bundles;
using Synker.UseCases.Clean;

namespace Synker.Cli.Commands
{
    /// <summary>
    /// Sync (import and export).
    /// </summary>
    [Command(Name = "clean", Description = "Clean bundles")]
    internal class Clean : ExportImportCommand
    {
        [Required]
        [Option("-md|--max-days", "Maximum days age for bundle.", CommandOptionType.SingleValue)]
        public int MaxDays { get; set; } = 14;

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            await base.OnExecuteAsync(app, console);
            var config = GetUserConfiguration();
            var profiles = await GetProfilesAsync(config);
            var bundleFactory = new ZipBundleFactory(config.BundlesDirectory);
            await new CleanCommand(profiles, bundleFactory).ExecuteAsync();
            return 0;
        }
    }
}
