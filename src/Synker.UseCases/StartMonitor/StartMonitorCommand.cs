using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synker.Core;
using Synker.UseCases.Export;
using Synker.UseCases.Import;

namespace Synker.UseCases.StartMonitor
{
    /// <summary>
    /// Command to start monitoring for settings source change. If new settings are received
    /// they will be automatically applied.
    /// </summary>
    public class StartMonitorCommand : ICommand<DelayActionRunner<Profile>>
    {
        public TimeSpan? ExecutionDelay { get; set; }

        public bool DisableImport { get; set; }

        public bool DisableExport { get; set; }

        private readonly IList<Profile> profiles;
        private readonly IBundleFactory bundleFactory;
        private static readonly ILogger<StartMonitorCommand> logger = AppLogger.Create<StartMonitorCommand>();

        public StartMonitorCommand(IList<Profile> profiles, IBundleFactory bundleFactory)
        {
            this.profiles = profiles;
            this.bundleFactory = bundleFactory;
        }

        /// <inheritdoc />
        public async Task<DelayActionRunner<Profile>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var monitorBundleFactory = bundleFactory as IBundleFactoryWithMonitor;
            if (monitorBundleFactory == null)
            {
                monitorBundleFactory = new NullBundleFactory();
            }

            // Export/import first.
            var profilesToMonitor = new List<Profile>();
            foreach (var profile in profiles)
            {
                try
                {
                    if (!DisableImport)
                    {
                        await new ImportCommand(profile, bundleFactory).ExecuteAsync(cancellationToken);
                    }
                    if (!DisableExport)
                    {
                        await new ExportCommand(profile, bundleFactory).ExecuteAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        $"Cannot import/export profile {profile.Name}, skip monitoring. Reason: {ex.Message}");
                    continue;
                }

                profilesToMonitor.Add(profile);
            }

            // Start monitoring for profiles.
            var delayActionRunner = new DelayActionRunner<Profile>(async p =>
                {
                    monitorBundleFactory.StopMonitor();
                    await new ExportCommand(p, bundleFactory).ExecuteAsync(cancellationToken);
                    monitorBundleFactory.StartMonitor();
                },
                delay: ExecutionDelay ?? TimeSpan.FromSeconds(3.0),
                failRetry: TimeSpan.FromSeconds(30.0)
            );
            if (!DisableExport)
            {
                foreach (var profile in profilesToMonitor)
                {
                    foreach (var target in profile.Targets)
                    {
                        if (target is ITargetWithMonitor monitorTarget)
                        {
                            monitorTarget.OnSettingsUpdate += (sender, t) => delayActionRunner.Queue(profile);
                        }
                    }

                    profile.StartMonitor();
                }

                delayActionRunner.Start();
            }

            // Start monitoring for settings source.
            if (!DisableImport)
            {
                monitorBundleFactory.OnSettingsUpdate += async (sender, profileId) =>
                {
                    var profileToImport = profiles.FirstOrDefault(p => p.Id == profileId);
                    if (profileToImport != null)
                    {
                        profileToImport.StopMonitor();
                        await new ImportCommand(profileToImport, bundleFactory).ExecuteAsync(cancellationToken);
                        profileToImport.StartMonitor();
                    }
                };
                monitorBundleFactory.StartMonitor();
            }

            return delayActionRunner;
        }
    }
}
