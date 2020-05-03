using System.Threading;
using System.Threading.Tasks;

namespace Synker.Desktop.Abstract
{
    /// <summary>
    /// The view model with initial async data loading feature.
    /// </summary>
    public interface IViewModelWithAsyncLoading
    {
        /// <summary>
        /// Load data.
        /// </summary>
        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
