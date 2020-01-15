using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synker.Domain
{
    /// <summary>
    /// Useless bundle factory, for testing only.
    /// </summary>
    public class NullBundleFactory : IBundleFactory, IBundleFactoryWithMonitor
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<BundleInfo>> GetAllAsync(string profileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<BundleInfo>>(
                new List<BundleInfo>());
        }

        /// <inheritdoc />
        public Task<IBundle> OpenAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IBundle>(new NullBundle());
        }

        /// <inheritdoc />
        public Task<IBundle> CreateAsync(string profileId, DateTime lastUpdateDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IBundle>(new NullBundle());
        }

        /// <inheritdoc />
        public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public event EventHandler<string> OnSettingsUpdate;

        /// <inheritdoc />
        public void StartMonitor()
        {
        }

        /// <inheritdoc />
        public void StopMonitor()
        {
        }
    }
}
