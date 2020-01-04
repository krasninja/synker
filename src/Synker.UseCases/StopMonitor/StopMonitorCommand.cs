using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synker.Core;

namespace Synker.UseCases.StopMonitor
{
    /// <summary>
    /// Command to stop monitoring. See <see cref="Synker.UseCases.StartMonitor" />.
    /// </summary>
    public class StopMonitorCommand : ICommand<bool>
    {
        private readonly IList<Profile> profiles;
        private readonly IBundleFactory bundleFactory;

        public StopMonitorCommand(IList<Profile> profiles, IBundleFactory bundleFactory)
        {
            this.profiles = profiles;
            this.bundleFactory = bundleFactory;
        }

        /// <inheritdoc />
        public Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var profile in profiles)
            {
                profile.StopMonitor();
            }

            if (bundleFactory is IBundleFactoryWithMonitor monitorBundleFactory)
            {
                monitorBundleFactory.StopMonitor();
            }

            return Task.FromResult(true);
        }
    }
}
