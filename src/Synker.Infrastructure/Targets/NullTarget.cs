using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Synker.Domain;

namespace Synker.Infrastructure.Targets
{
    /// <summary>
    /// Target that does nothing. For testing only.
    /// The target name in profile file must be "null".
    /// </summary>
    public class NullTarget : TargetBase
    {
        /// <inheritdoc />
        public override IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext)
        {
            return Setting.EmptySettings.ToAsyncEnumerable();
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
