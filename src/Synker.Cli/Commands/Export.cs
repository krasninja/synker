using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Infrastructure.Bundles;
using Synker.UseCases.Export;

namespace Synker.Cli.Commands
{
    /// <summary>
    /// Export settings from local applications.
    /// </summary>
    [Command(Name = "export", Description = "Export settings from local applications")]
    internal class Export : ExportImportCommand
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
                var command = new ExportCommand(profile, bundleFactory)
                {
                    Force = Force
                };
                try
                {
                    await command.ExecuteAsync();
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
