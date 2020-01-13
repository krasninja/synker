using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Synker.Core;

namespace Synker.Common.Targets
{
    /// <summary>
    /// Ensures that files exist. Otherwise cancels export or import.
    /// </summary>
    public class EnsureFilesTarget : TargetBase
    {
        /// <summary>
        /// Files that must exist.
        /// </summary>
        [Required]
        public IList<string> Files { get; set; } = new List<string>();

        /// <inheritdoc />
        public override async IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext)
        {
            syncContext.CancelProcessing = !EnsureAllFilesExist();
            yield return Setting.EmptySetting;
        }

        /// <inheritdoc />
        public override Task ImportAsync(SyncContext syncContext, IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            syncContext.CancelProcessing = !EnsureAllFilesExist();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task<DateTime?> GetLastUpdateDateTimeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((DateTime?)null);
        }

        private bool EnsureAllFilesExist()
        {
            foreach (string file in Files)
            {
                if (!File.Exists(file))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
