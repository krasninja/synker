using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synker.Core;

namespace Synker.Common.Targets
{
    /// <summary>
    /// Target that does nothing. For testing only.
    /// </summary>
    public class NullTarget : TargetBase
    {
        /// <inheritdoc />
        public override async IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext)
        {
            yield break;
        }

        /// <inheritdoc />
        public override Task ImportAsync(SyncContext syncContext, IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task<DateTime?> GetLastUpdateDateTimeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<DateTime?>(DateTime.Now);
        }
    }
}
