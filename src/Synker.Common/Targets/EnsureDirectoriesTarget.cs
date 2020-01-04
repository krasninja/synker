using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Synker.Core;

namespace Synker.Common.Targets
{
    /// <summary>
    /// Ensures that directories exist. Otherwise cancels export or import.
    /// </summary>
    public class EnsureDirectoriesTarget : TargetBase
    {
        /// <summary>
        /// Directories that must exist.
        /// </summary>
        [Required]
        public IList<string> Directories { get; } = new List<string>();

        /// <inheritdoc />
        public override IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return new AsyncEnumerable<Setting>(async yield =>
            {
                syncContext.CancelProcessing = !EnsureAllDirectoriesExist();
                await yield.ReturnAsync(Setting.EmptySetting);
            });
        }

        /// <inheritdoc />
        public override Task ImportAsync(SyncContext syncContext, IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            syncContext.CancelProcessing = !EnsureAllDirectoriesExist();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task<DateTime?> GetLastUpdateDateTimeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((DateTime?)null);
        }

        private bool EnsureAllDirectoriesExist()
        {
            foreach (string directory in Directories)
            {
                if (!Directory.Exists(directory))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
