using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Saritasa.Tools.Domain;
using YamlDotNet.Serialization;

namespace Synker.Domain
{
    /// <summary>
    /// Application configuration information.
    /// </summary>
    public class Profile
    {
        public const string Key_Type = "type";
        public const string Key_LastUpdate = "last-update";
        public const string Key_Hostname = "hostname";

        /// <summary>
        /// Unique short name.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Profile full name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Target to be performed on export and import.
        /// </summary>
        [YamlIgnore]
        public IList<ITarget> Targets { get; } = new List<ITarget>();

        private static readonly ILogger<Profile> logger = AppLogger.Create<Profile>();

        /// <summary>
        /// Add multiple targets.
        /// </summary>
        /// <param name="targets">Targets to add.</param>
        public void AddTargets(IList<ITarget> targets)
        {
            foreach (ITarget target in targets)
            {
                this.Targets.Add(target);
            }
        }

        public ValidationErrors ValidateTargets()
        {
            foreach (ITarget target in Targets)
            {
                var targetValidationResults = ValidationErrors.CreateFromObjectValidation(target);
                if (targetValidationResults.HasErrors)
                {
                    var error = targetValidationResults.First();
                    logger.LogInformation($"[{target.Id}] {error.Key}: {error.Value.First()}.");
                    return targetValidationResults;
                }
            }
            return new ValidationErrors();
        }

        /// <summary>
        /// Get date and time of latest settings update by application.
        /// It goes thru all targets and gets latest date. If it cannot be determined the
        /// null is returned.
        /// </summary>
        /// <returns>Date and time or null.</returns>
        public async Task<DateTime?> GetLatestLocalUpdateDateTimeAsync()
        {
            DateTime? latestSettingsDate = null;
            var syncContext = new SyncContext();
            foreach (ITarget target in Targets)
            {
                var date = await target.GetLastUpdateDateTimeAsync(syncContext);
                if (syncContext.CancelProcessing)
                {
                    break;
                }
                if (!latestSettingsDate.HasValue || date > latestSettingsDate)
                {
                    latestSettingsDate = date;
                }
            }
            return latestSettingsDate;
        }

        public async Task<DateTime?> GetLatestBundleUpdateDateTimeAsync(IBundle bundle)
        {
            if (bundle == null)
            {
                throw new ArgumentNullException(nameof(bundle));
            }

            DateTime? latestBundleDate = null;
            foreach (ITarget target in this.Targets)
            {
                var metadata = await bundle.GetMetadataAsync(target.Id);
                if (metadata.ContainsKey(Key_LastUpdate))
                {
                    var ticks = long.Parse(metadata[Key_LastUpdate]);
                    var lastBundleUpdateDate = new DateTime(ticks, DateTimeKind.Utc);
                    if (!latestBundleDate.HasValue || lastBundleUpdateDate > latestBundleDate)
                    {
                        latestBundleDate = lastBundleUpdateDate;
                    }
                }
            }
            return latestBundleDate;
        }

        public static string GetTargetName(Type type) =>
            type.Name.Substring(0, type.Name.LastIndexOf("Target", StringComparison.OrdinalIgnoreCase)).ToLower();

        #region Monitoring

        /// <summary>
        /// Start all targets monitors.
        /// </summary>
        public void StartMonitor()
        {
            foreach (var target in Targets)
            {
                if (target is ITargetWithMonitor targetMonitor)
                {
                    targetMonitor.StartMonitor();
                }
            }
        }

        /// <summary>
        /// Stop all targets monitors.
        /// </summary>
        public void StopMonitor()
        {
            foreach (var target in Targets)
            {
                if (target is ITargetWithMonitor targetMonitor)
                {
                    targetMonitor.StopMonitor();
                }
            }
        }

        #endregion
    }
}
