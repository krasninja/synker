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
    public class NullSettingsTarget : Target
    {
        /// <inheritdoc />
        public override IAsyncEnumerable<Setting> ExportAsync()
        {
            return Setting.EmptySettings.ToAsyncEnumerable();
        }

        /// <inheritdoc />
        public override Task ImportAsync(IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task<DateTime?> GetUpdateDateTimeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DateTime?>(DateTime.Now);
        }
    }
}
