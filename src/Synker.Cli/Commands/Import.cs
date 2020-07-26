using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Infrastructure.Bundles;
using Synker.UseCases.Import;

namespace Synker.Cli.Commands
{
    /// <summary>
    /// Import settings to local applications.
    /// </summary>
    [Command(Name = "import", Description = "Import settings to local applications")]
    internal class Import : ExportImportCommand
    {
        /// <inheritdoc />
        protected override async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            await base.OnExecuteAsync(app, console);
            var config = GetUserConfiguration();
            var profiles = await GetProfilesAsync(config);
            var bundleFactory = new ZipBundleFactory(config.BundlesDirectory);
            foreach (var profile in profiles)
            {
                try
                {
                    await new ImportCommand(profile, bundleFactory)
                    {
                        Force = Force
                    }.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    await console.Error.WriteLineAsync(ex.Message);
                }
            }
            return 0;
        }
    }
}
