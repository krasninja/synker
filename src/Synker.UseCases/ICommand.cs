using System.Threading;
using System.Threading.Tasks;

namespace Synker.UseCases
{
    /// <summary>
    /// Command or application use case.
    /// </summary>
    public interface ICommand<T>
    {
        /// <summary>
        /// Execute command.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to monitor and cancel the request.</param>
        /// <returns>Command result.</returns>
        Task<T> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
