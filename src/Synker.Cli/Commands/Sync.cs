using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Common.Bundles;
using Synker.Common.ProfileLoaders;
using Synker.Core;
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
            var filesProfileLoader = new FilesProfileLoader(Profiles);
            var profiles = await ProfileFactory.LoadAsync(filesProfileLoader, ProfilesExclude);
            var bundleFactory = new ZipBundleFactory(Bundles);
            foreach (var profile in profiles)
            {
                await new ImportCommand(profile, bundleFactory).ExecuteAsync();
                await new ExportCommand(profile, bundleFactory).ExecuteAsync();
            }
            return 0;
        }
    }
}
