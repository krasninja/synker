using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Infrastructure.Bundles;
using Synker.Infrastructure.ProfileLoaders;
using Synker.Domain;
using Synker.UseCases.Clean;

namespace Synker.Cli.Commands
{
    /// <summary>
    /// Sync (import and export).
    /// </summary>
    [Command(Name = "clean", Description = "Clean bundles")]
    internal class Clean : ExportImportBase
    {
        [Required]
        [Option("-md|--max-days", "Maximum days age for bundle.", CommandOptionType.SingleValue)]
        public int MaxDays { get; set; } = 14;

        public async Task<int> OnExecuteAsync()
        {
            var config = GetUserConfiguration();
            var profiles = await GetProfilesAsync(config);
            var bundleFactory = new ZipBundleFactory(config.BundlesDirectory);
            await new CleanCommand(profiles, bundleFactory).ExecuteAsync();
            return 0;
        }
    }
}
