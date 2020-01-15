using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synker.Domain
{
    /// <summary>
    /// Extensions for <see cref="IBundleFactory" />.
    /// </summary>
    public static class BundleFactoryExtensions
    {
        /// <summary>
        /// Get latest bundle item from bundle.
        /// </summary>
        /// <param name="bundleFactory">Bundle factory.</param>
        /// <param name="profileId">Profile identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Latest bundle item or null.</returns>
        public static async Task<BundleInfo> GetLatestAsync(
            this IBundleFactory bundleFactory,
            string profileId,
            CancellationToken cancellationToken = default)
        {
            return (await bundleFactory.GetAllAsync(profileId, cancellationToken)).LastOrDefault();
        }
    }
}
