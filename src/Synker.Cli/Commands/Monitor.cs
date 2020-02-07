using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Synker.Infrastructure.Bundles;
using Synker.UseCases.StartMonitor;
using Synker.UseCases.StopMonitor;

namespace Synker.Cli.Commands
{
    [Command(Name = "monitor", Description = "Run the daemon to automatically sync settings.")]
    internal class Monitor : ExportImportCommand
    {
        private static readonly AutoResetEvent closeEvent = new AutoResetEvent(false);

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            await base.OnExecuteAsync(app, console);
            var config = GetUserConfiguration();
            var profiles = await GetProfilesAsync(config);
            var bundleFactory = new ZipBundleFactory(config.BundlesDirectory);

            // Start monitor.
            console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                closeEvent.Set();
            };
            var startMonitorCommand = new StartMonitorCommand(profiles, bundleFactory);
            await startMonitorCommand.ExecuteAsync();
            console.WriteLine("Monitor started, press CTRL-C to cancel.");
            closeEvent.WaitOne();

            // Stop monitor.
            var stopMonitorCommand = new StopMonitorCommand(profiles, bundleFactory);
            await stopMonitorCommand.ExecuteAsync();
            return 0;
        }
    }
}
