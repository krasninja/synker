using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Common.Bundles;
using Synker.Common.ProfileLoaders;
using Synker.Core;
using Synker.UseCases.Export;

namespace Synker.Cli.Commands
{
    /// <summary>
    /// Export settings from local applications.
    /// </summary>
    [Command(Name = "export", Description = "Export settings from local applications")]
    internal class Export : ExportImportBase
    {
        [Option("-f|--force", "Force export.", CommandOptionType.NoValue)]
        public bool Force { get; set; } = false;

        public async Task<int> OnExecuteAsync()
        {
            var filesProfileLoader = new FilesProfileLoader(Profiles);
            var profiles = await ProfileFactory.LoadAsync(filesProfileLoader, ProfilesExclude);
            var bundleFactory = new ZipBundleFactory(Bundles);
            foreach (var profile in profiles)
            {
                var command = new ExportCommand(profile, bundleFactory)
                {
                    Force = Force
                };
                await command.ExecuteAsync();
            }
            return 0;
        }
    }
}
