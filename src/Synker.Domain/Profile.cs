using System;
using System.Collections;
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

        private readonly List<Target> targets = new List<Target>();

        /// <summary>
        /// Targets to be performed on export and import.
        /// </summary>
        [YamlIgnore]
        public IReadOnlyList<Target> Targets => targets.AsReadOnly();

        private static readonly ILogger<Profile> logger = AppLogger.Create<Profile>();
        private int targetIndex = 0;

        /// <summary>
        /// Add target.
        /// </summary>
        /// <param name="id">Target identifier within profile.</param>
        /// <param name="target">Target to add.</param>
        public Target AddTarget(string id, Target target)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            this.targets.Add(target);
            return target;
        }

        /// <summary>
        /// Add target. The target id will be auto-assigned.
        /// </summary>
        /// <param name="target">Target to add.</param>
        public Target AddTarget(Target target)
        {
            if (string.IsNullOrEmpty(target.Id))
            {
                target.Id = (targetIndex++).ToString("000");
            }
            this.targets.Add(target);
            return target;
        }

        public ValidationErrors ValidateTargets()
        {
            foreach (var target in Targets)
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
            foreach (var target in Targets)
            {
                var condition = await target.GetFirstNonSatisfyConditionAsync();
                if (condition != null)
                {
                    continue;
                }
                var date = await target.GetUpdateDateTimeAsync();
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
            foreach (var target in this.Targets)
            {
                var condition = await target.GetFirstNonSatisfyConditionAsync();
                if (condition != null)
                {
                    continue;
                }
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
                if (target is IMonitorTarget targetMonitor)
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
                if (target is IMonitorTarget targetMonitor)
                {
                    targetMonitor.StopMonitor();
                }
            }
        }

        #endregion
    }
}
