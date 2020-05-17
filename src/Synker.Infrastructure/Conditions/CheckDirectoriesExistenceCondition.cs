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
    /// Ensures that directories exist.
    /// </summary>
    public class CheckDirectoriesExistenceCondition : Condition
    {
        /// <summary>
        /// Directories that must exist.
        /// </summary>
        [Required]
        public IList<string> Directories { get; set; } = new string[] { };

        private readonly ILogger<CheckDirectoriesExistenceCondition> logger = AppLogger.Create<CheckDirectoriesExistenceCondition>();

        /// <inheritdoc />
        public override Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default)
        {
            foreach (string directory in Directories)
            {
                var exist = Directory.Exists(directory);
                logger.LogTrace("Verify directory existence {directory} - {exist}.", directory, exist);
                if (!exist)
                {
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(true);
        }
    }
}
