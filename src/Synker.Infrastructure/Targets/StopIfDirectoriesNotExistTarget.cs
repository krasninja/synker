using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Synker.Domain;

namespace Synker.Infrastructure.Targets
{
    /// <summary>
    /// Ensures that directories exist. Otherwise cancels export or import.
    /// </summary>
    public class StopIfDirectoriesNotExistTarget : TargetBase
    {


        /// <summary>
        /// Directories that must exist.
        /// </summary>
        [Required]
        public IList<string> Directories { get; } = new List<string>();

        /// <inheritdoc />
        public override IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext)
            => Setting.EmptySettings.ToAsyncEnumerable();

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