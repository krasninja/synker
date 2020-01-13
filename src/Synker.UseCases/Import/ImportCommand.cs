using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synker.Core;

namespace Synker.UseCases.Import
{
    /// <summary>
    /// Command to import settings from source.
    /// </summary>
    public class ImportCommand : ICommand<bool>
    {
        public bool Force { get; set; }

        private readonly Profile profile;
        private readonly IBundleFactory bundleFactory;
        private static readonly ILogger<ImportCommand> logger = AppLogger.Create<ImportCommand>();

        public ImportCommand(Profile profile, IBundleFactory bundleFactory)
        {
            this.profile = profile;
            this.bundleFactory = bundleFactory;
        }

        /// <inheritdoc />
        public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation($"Start import for profile {profile.Id} - {profile.Name}.");

            // Latest local settings date.
            var latestLocalUpdateDateTime = await profile.GetLatestLocalUpdateDateTimeAsync();

            // Then try to find latest bundle.
            var latestBundleInfo = await bundleFactory.GetLatestAsync(profile.Id, cancellationToken);
            if (latestBundleInfo == null)
            {
                logger.LogInformation($"No bundles found for profile {profile.Id}");
                return false;
            }

            // Check is external settings are newer.
            var latestBundle = await bundleFactory.OpenAsync(latestBundleInfo.Id, cancellationToken);
            var latestBundleUpdateDateTime = await profile.GetLatestBundleUpdateDateTimeAsync(latestBundle);
            if (latestLocalUpdateDateTime.HasValue && latestLocalUpdateDateTime >= latestBundleUpdateDateTime && !Force)
            {
                logger.LogInformation("Skip import because current settings date " +
                                      $"{latestLocalUpdateDateTime} equal or newer than date " +
                                      $"{latestBundleUpdateDateTime} of bundle {latestBundleInfo.Id}.");
                return false;
            }

            // Import by targets.
            try
            {
                var syncContext = new SyncContext();
                foreach (ITarget target in profile.Targets)
                {
                    await target.ImportAsync(
                        syncContext,
                        latestBundle.GetSettingsAsync(target.Id),
                        cancellationToken);

                    if (syncContext.CancelProcessing)
                    {
                        logger.LogInformation($"Target {target.Id} requested cancel import processing.");
                        return false;
                    }
                }
            }
            finally
            {
                ((IDisposable) latestBundle)?.Dispose();
            }

            return true;
        }
    }
}
