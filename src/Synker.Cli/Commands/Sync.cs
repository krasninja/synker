using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Infrastructure.Bundles;
using Synker.UseCases.Export;
using Synker.UseCases.Import;

namespace Synker.Cli.Commands
{
    /// <summary>
    /// Sync (import and export).
    /// </summary>
    [Command(Name = "sync", Description = "Import and export settings")]
    internal class Sync : ExportImportBase
    {
        public async Task<int> OnExecuteAsync()
        {
            var config = GetUserConfiguration();
            var profiles = await GetProfilesAsync(config);
            var bundleFactory = new ZipBundleFactory(config.BundlesDirectory);
            foreach (var profile in profiles)
            {
                await new ImportCommand(profile, bundleFactory).ExecuteAsync();
                await new ExportCommand(profile, bundleFactory).ExecuteAsync();
            }
            return 0;
        }
    }
}
