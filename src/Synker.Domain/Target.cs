using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synker.Domain
{
    /// <summary>
    /// Target wrapper that contains additional features like id, conditions, etc.
    /// </summary>
    public abstract class Target
    {
        /// <summary>
        /// Target unique identifier within profile.
        /// </summary>
        public string Id { get; internal set; }

        private readonly List<Condition> conditions = new List<Condition>();
        private int conditionIndex = 0;

        /// <summary>
        /// Conditions to process the target.
        /// </summary>
        public IReadOnlyCollection<Condition> Conditions => conditions.AsReadOnly();

        public Condition AddCondition(string id, Condition condition)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            condition.Id = id;
            this.conditions.Add(condition);
            return condition;
        }

        public Condition AddCondition(Condition condition)
        {
            if (string.IsNullOrEmpty(condition.Id))
            {
                condition.Id = (conditionIndex++).ToString("000");
            }
            this.conditions.Add(condition);
            return condition;
        }

        public async Task<Condition> GetFirstNonSatisfyConditionAsync(CancellationToken cancellationToken = default)
        {
            foreach (Condition condition in Conditions)
            {
                if (!await condition.IsSatisfiedAsync(cancellationToken))
                {
                    return condition;
                }
            }
            return null;
        }

        #region Abstract

        /// <summary>
        /// Export settings.
        /// </summary>
        /// <returns>Settings entries.</returns>
        public abstract IAsyncEnumerable<Setting> ExportAsync();

        /// <summary>
        /// Import settings.
        /// </summary>
        /// <param name="settings">Settings entries.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Task.</returns>
        public abstract Task ImportAsync(IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get last update date/time of settings in UTC format.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Last update date time or null if it is not available.</returns>
        public abstract Task<DateTime?> GetUpdateDateTimeAsync(CancellationToken cancellationToken = default);

        #endregion
    }
}
