using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synker.Domain;

namespace Synker.UseCases.Clean
{
    /// <summary>
    /// Clean old bundles.
    /// </summary>
    public class CleanCommand : ICommand<bool>
    {
        public int MaxDays { get; set; } = 14;

        private readonly IList<Profile> profiles;
        private readonly IBundleFactory bundleFactory;
        private static readonly ILogger<CleanCommand> logger = AppLogger.Create<CleanCommand>();

        public CleanCommand(IList<Profile> profiles, IBundleFactory bundleFactory)
        {
            this.profiles = profiles;
            this.bundleFactory = bundleFactory;
        }

        /// <inheritdoc />
        public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Today;
            foreach (Profile profile in profiles)
            {
                var allItems = await bundleFactory.GetAllAsync(profile.Id, cancellationToken);
                foreach (BundleInfo bundleItem in allItems.Where(bi => bi.IsOutdated(now, MaxDays)))
                {
                    await bundleFactory.RemoveAsync(bundleItem.Id, cancellationToken);
                }
            }
            return true;
        }
    }
}
