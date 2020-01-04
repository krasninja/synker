using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Common.Bundles;
using Synker.Common.ProfileLoaders;
using Synker.Core;
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
            var filesProfileLoader = new FilesProfileLoader(Profiles);
            var profiles = await ProfileFactory.LoadAsync(filesProfileLoader, ProfilesExclude);
            var bundleFactory = new ZipBundleFactory(Bundles);
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
