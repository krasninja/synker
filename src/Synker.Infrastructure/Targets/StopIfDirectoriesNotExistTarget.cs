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
    /// Ensures that directories exist. Otherwise cancels export or import.
    /// The target name in profile file must be "stop-if-directories-not-exist".
    /// </summary>
    public class StopIfDirectoriesNotExistTarget : TargetBase
    {
        /// <summary>
        /// Directories that must exist.
        /// </summary>
        [Required]
        public IList<string> Directories { get; set; } = new List<string>();

        private readonly ILogger<StopIfFilesNotExistTarget> logger = AppLogger.Create<StopIfFilesNotExistTarget>();

        /// <inheritdoc />
        public override IAsyncEnumerable<Setting> ExportAsync(SyncContext syncContext)
        {
            syncContext.CancelProcessing = !EnsureAllDirectoriesExist();
            return Setting.EmptySettings.ToAsyncEnumerable();
        }

        /// <inheritdoc />
        public override Task ImportAsync(SyncContext syncContext, IAsyncEnumerable<Setting> settings,
            CancellationToken cancellationToken)
        {
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
                var exist = Directory.Exists(directory);
                logger.LogTrace("Verify directory existence {directory} - {exist}.", directory, exist);
                if (!exist)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
