using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Infrastructure.Bundles;
using Synker.Infrastructure.ProfileLoaders;
using Synker.Domain;
using Synker.UseCases.Import;

namespace Synker.Cli.Commands
{
    /// <summary>
    /// Import settings to local applications.
    /// </summary>
    [Command(Name = "import", Description = "Import settings from local applications")]
    internal class Import : ExportImportBase
    {
        [Option("-f|--force", "Force import.", CommandOptionType.NoValue)]
        public bool Force { get; set; }

        public async Task<int> OnExecuteAsync()
        {
            var config = GetUserConfiguration();
            var profiles = await GetProfilesAsync(config);
            var bundleFactory = new ZipBundleFactory(config.BundlesDirectory);
            foreach (var profile in profiles)
            {
                await new ImportCommand(profile, bundleFactory)
                {
                    Force = Force
                }.ExecuteAsync();
            }
            return 0;
        }
    }
}
