using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synker.Domain;

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

            // Validate profile targets.
            var errors = profile.ValidateTargets();
            if (errors.HasErrors)
            {
                logger.LogInformation($"Import for {profile.Id} skipped because of errors above.");
                return false;
            }

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
            if (!latestLocalUpdateDateTime.HasValue && !Force)
            {
                logger.LogInformation("Skip import because cannot get latest local settings update" +
                                      " date and time. Use Force to force import.");
            }
            if (latestLocalUpdateDateTime >= latestBundleUpdateDateTime && !Force)
            {
                logger.LogInformation("Skip import because current settings date " +
                                      $"{latestLocalUpdateDateTime} equal or newer than date " +
                                      $"{latestBundleUpdateDateTime} of bundle {latestBundleInfo.Id}.");
                return false;
            }
            else
            {
                logger.LogTrace($"Latest local settings date: {latestLocalUpdateDateTime}", latestLocalUpdateDateTime);
                logger.LogTrace($"Latest bundle settings date: {latestBundleUpdateDateTime}", latestBundleUpdateDateTime);
            }

            // Import by targets.
            try
            {
                foreach (var target in profile.Targets)
                {
                    var notSatisfyCondition = await target.GetFirstNonSatisfyConditionAsync();
                    if (notSatisfyCondition != null)
                    {
                        logger.LogInformation($"Target \"{target.Id}\" skipped because of condition {notSatisfyCondition.GetType().Name}.");
                        continue;
                    }
                    await target.ImportAsync(
                        latestBundle.GetSettingsAsync(target.Id),
                        cancellationToken);
                }
            }
            finally
            {
                (latestBundle as IDisposable)?.Dispose();
            }

            return true;
        }
    }
}
