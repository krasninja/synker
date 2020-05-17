using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synker.Domain;

namespace Synker.Infrastructure.Conditions
{
    /// <summary>
    /// Ensures that files exist.
    /// </summary>
    public class CheckFilesExistenceCondition : Condition
    {
        /// <summary>
        /// Files that must exist.
        /// </summary>
        [Required]
        public IList<string> Files { get; set; } = new string[] { };

        private readonly ILogger<CheckFilesExistenceCondition> logger = AppLogger.Create<CheckFilesExistenceCondition>();

        /// <inheritdoc />
        public override Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            foreach (string file in Files)
            {
                var exist = File.Exists(file);
                logger.LogTrace("Verify file existence {file} - {exist}.", file, exist);
                if (!exist)
                {
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(true);
        }
    }
}
