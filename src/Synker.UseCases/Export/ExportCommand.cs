using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Saritasa.Tools.Domain.Exceptions;
using Synker.Domain;
using Synker.UseCases.Common;

namespace Synker.UseCases.Export
{
    /// <summary>
    /// Command to export settings.
    /// </summary>
    public class ExportCommand : ICommand<ExportResult>
    {
        public bool Force { get; set; }

        private readonly Profile profile;
        private readonly IBundleFactory bundleFactory;
        private static readonly ILogger<ExportCommand> logger = AppLogger.Create<ExportCommand>();

        public ExportCommand(Profile profile, IBundleFactory bundleFactory)
        {
            this.profile = profile;
            this.bundleFactory = bundleFactory;
        }

        /// <inheritdoc />
        public async Task<ExportResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation($"Start export for profile {profile.Id} ({profile.Name}).");

            // Validate profile targets.
            var errors = profile.ValidateTargets();
            if (errors.HasErrors)
            {
                logger.LogInformation($"Export for {profile.Id} skipped because of errors above.");
                throw new ValidationException(errors);
            }

            // Get required data, validation.
            var latestLocalUpdateDateTime = await profile.GetLatestLocalUpdateDateTimeAsync();
            if (!latestLocalUpdateDateTime.HasValue)
            {
                logger.LogWarning($"Cannot get latest local settings update date for profile {profile.Id}, skipping.");
                throw new ValidationException(errors);
            }

            // We allow export only if local settings date is higher than bundle date.
            var latestBundleInfo = await bundleFactory.GetLatestAsync(profile.Id, cancellationToken);
            if (latestBundleInfo != null)
            {
                var bundle = await bundleFactory.OpenAsync(latestBundleInfo.Id, cancellationToken);
                var latestBundleUpdateDateTime = await profile.GetLatestBundleUpdateDateTimeAsync(bundle);
                if (latestBundleUpdateDateTime >= latestLocalUpdateDateTime && !Force)
                {
                    logger.LogInformation("Skip export because current settings date " +
                                          $"{latestLocalUpdateDateTime} equal " +
                                          $"or older than date {latestBundleUpdateDateTime} of bundle {bundle.Id}.");
                    return ExportResult.SettingsDateOlderThanBundleDate;
                }
            }

            // We are ready to export.
            var lazyBundle = new AsyncLazy<IBundle>(
                () => bundleFactory.CreateAsync(
                    profile.Id,
                    latestLocalUpdateDateTime.Value,
                    cancellationToken)
                );
            try
            {
                await ExportTargetsObjects(
                    profile.Targets,
                    lazyBundle,
                    latestLocalUpdateDateTime.Value,
                    cancellationToken);
            }
            catch (Exception)
            {
                await lazyBundle.ExecuteAsync(
                    async b => await bundleFactory.RemoveAsync(b.Id, cancellationToken)
                );
                throw;
            }
            finally
            {
                lazyBundle.Dispose();
            }
            return ExportResult.Success;
        }

        private async Task<int> ExportTargetsObjects(
            IReadOnlyList<Target> targets,
            AsyncLazy<IBundle> lazyBundle,
            DateTime latestLocalUpdateDateTime,
            CancellationToken cancellationToken)
        {
            // Save targets.
            int settingIndex = 0;
            foreach (var target in targets)
            {
                var targetValidationResults = Saritasa.Tools.Domain.ValidationErrors.CreateFromObjectValidation(target);
                if (targetValidationResults.HasErrors)
                {
                    throw new ValidationException(targetValidationResults);
                }
                var notSatisfyCondition = await target.GetFirstNonSatisfyConditionAsync(cancellationToken);
                if (notSatisfyCondition != null)
                {
                    logger.LogInformation($"Target {target.Id} skipped because of condition {notSatisfyCondition.GetType().Name}.");
                    continue;
                }

                // Save target settings.
                var settingsAsyncCollection = target.ExportAsync();
                await foreach (var setting in settingsAsyncCollection.WithCancellation(cancellationToken))
                {
                    if (setting == Setting.EmptySetting)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(setting.Id))
                    {
                        setting.Id = settingIndex.ToString("000");
                    }

                    await (await lazyBundle.CreateOrGetAsync()).PutSettingAsync(setting, target.Id, cancellationToken);
                    settingIndex++;
                }

                if (settingIndex > 0)
                {
                    // Save target metadata.
                    await (await lazyBundle.CreateOrGetAsync()).PutMetadataAsync(new Dictionary<string, string>
                    {
                        [Profile.Key_Type] = Profile.GetTargetName(target.GetType()),
                        [Profile.Key_LastUpdate] =
                            latestLocalUpdateDateTime.ToUniversalTime().Ticks.ToString()
                    }, target.Id, cancellationToken);
                }
            }

            if (settingIndex > 0)
            {
                await (await lazyBundle.CreateOrGetAsync()).PutMetadataAsync(
                    new Dictionary<string, string>
                    {
                        [Profile.Key_Hostname] = Environment.MachineName
                    },
                    targetId: string.Empty,
                    cancellationToken);
            }

            return settingIndex;
        }
    }
}
