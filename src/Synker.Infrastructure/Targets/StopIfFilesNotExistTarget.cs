using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synker.Domain;

namespace Synker.Infrastructure.Targets
{
    /// <summary>
    /// Ensures that files exist. Otherwise cancels export or import.
    /// The target name in profile file must be "stop-if-files-not-exist".
    /// </summary>
    public class StopIfFilesNotExistTarget : TargetBase
    {
        /// <summary>
        /// Files that must exist.
        /// </summary>
        [Required]
        public IList<string> Files { get; set; } = new List<string>();

        private readonly ILogger<StopIfFilesNotExistTarget> logger = AppLogger.Create<StopIfFilesNotExistTarget>();

        /// <inheritdoc />
        public override IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext)
        {
            syncContext.CancelProcessing = !EnsureAllFilesExist();
            return Setting.EmptySettings.ToAsyncEnumerable();
        }

        /// <inheritdoc />
        public override Task ImportAsync(SyncContext syncContext, IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken)
        {
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
                var exist = File.Exists(file);
                logger.LogTrace("Verify file existence {file} - {exist}.", file, exist);
                if (!exist)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
