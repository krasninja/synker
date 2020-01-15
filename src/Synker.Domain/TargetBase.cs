using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synker.Domain
{
    /// <summary>
    /// Base target class.
    /// </summary>
    public abstract class TargetBase : ITarget
    {
        /// <inheritdoc />
        public string Id { get; internal set; }

        /// <inheritdoc />
        public bool Enabled { get; internal set; } = true;

        /// <inheritdoc />
        public abstract IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext);

        /// <inheritdoc />
        public abstract Task ImportAsync(SyncContext syncContext, IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract Task<DateTime?> GetLastUpdateDateTimeAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public override string ToString() => Id;
    }
}
