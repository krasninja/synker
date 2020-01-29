using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synker.Domain
{
    /// <summary>
    /// Abstract setting entity that can be imported and exported across hosts.
    /// </summary>
    public interface ITarget
    {
        /// <summary>
        /// Target identifier.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Is target enabled or should be skipped.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Export settings.
        /// </summary>
        /// <param name="syncContext">Synchronization context.</param>
        /// <returns>Settings entries.</returns>
        IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext);

        /// <summary>
        /// Import settings.
        /// </summary>
        /// <param name="syncContext">Synchronization context.</param>
        /// <param name="settings">Settings entries.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Task.</returns>
        Task ImportAsync(SyncContext syncContext, IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get last update date/time of settings in UTC format.
        /// </summary>
        /// <param name="syncContext">Synchronization context.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Last update date time or null if it is not available.</returns>
        Task<DateTime?> GetLastUpdateDateTimeAsync(SyncContext syncContext,
            CancellationToken cancellationToken = default);
    }
}
