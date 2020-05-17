using System;
using System.Threading.Tasks;

namespace Synker.UseCases.Common
{
    /// <summary>
    /// Async lazy with additional functionality.
    /// </summary>
    /// <typeparam name="T">Target object type.</typeparam>
    public sealed class AsyncLazy<T> : IDisposable where T : class
    {
        private readonly Func<Task<T>> func;
        private T cached;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="func">Delegate to create object.</param>
        public AsyncLazy(Func<Task<T>> func)
        {
            this.func = func;
        }

        /// <summary>
        /// Create or get cached object.
        /// </summary>
        /// <returns>Object.</returns>
        public async Task<T> CreateOrGetAsync() => cached ??= await func();

        /// <summary>
        /// Execute action on object if it has been created.
        /// </summary>
        /// <param name="action">Action.</param>
        /// <returns><c>True</c> if has been executed.</returns>
        public bool Execute(Action<T> action)
        {
            if (cached != null)
            {
                action(cached);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Execute action on object if it has been created.
        /// </summary>
        /// <param name="action">Action.</param>
        /// <returns><c>True</c> if has been executed.</returns>
        public async Task<bool> ExecuteAsync(Func<T, Task> action)
        {
            if (cached != null)
            {
                await action(cached);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (cached is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
