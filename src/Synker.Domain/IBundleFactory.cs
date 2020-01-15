using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synker.Domain
{
    /// <summary>
    /// Bundles factory. Used to get, open and create bundles.
    /// </summary>
    public interface IBundleFactory
    {
        /// <summary>
        /// Get all bundles for specific profile.
        /// </summary>
        /// <param name="profileId">Profile identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Bundle info items.</returns>
        Task<IReadOnlyList<BundleInfo>> GetAllAsync(string profileId, CancellationToken cancellationToken =
            default);

        /// <summary>
        /// Open bundle settings.
        /// </summary>
        /// <param name="id">Bundle identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Bundle.</returns>
        Task<IBundle> OpenAsync(string id, CancellationToken cancellationToken =
            default);

        /// <summary>
        /// Create settings bundle.
        /// </summary>
        /// <param name="profileId">Profile identifier.</param>
        /// <param name="lastUpdateDate">Last update date of local settings.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Created bundle.</returns>
        Task<IBundle> CreateAsync(string profileId, DateTime lastUpdateDate, CancellationToken cancellationToken =
            default);

        /// <summary>
        /// Remove bundle.
        /// </summary>
        /// <param name="id">Bundle identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Task.</returns>
        Task RemoveAsync(string id, CancellationToken cancellationToken =
            default);
    }
}
