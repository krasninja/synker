using System;
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
    internal class Sync : ExportImportCommand
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
                    await new ImportCommand(profile, bundleFactory).ExecuteAsync();
                }
                catch (Exception ex)
                {
                    await console.Error.WriteLineAsync(ex.Message);
                }

                try
                {
                    await new ExportCommand(profile, bundleFactory).ExecuteAsync();
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
