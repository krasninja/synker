using System.Threading;
using System.Threading.Tasks;

namespace Synker.Domain
{
    /// <summary>
    /// The condition that can be applied to a target. If it is not satisfied
    /// the target will not be processed.
    /// </summary>
    public abstract class Condition
    {
        /// <summary>
        /// Condition unique identifier within profile.
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// Is condition satisfied.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns><c>True</c> if condition is satisfied, <c>false</c> otherwise.</returns>
        public abstract Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default);
    }
}
