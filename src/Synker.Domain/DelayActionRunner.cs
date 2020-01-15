using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Synker.Domain
{
    /// <summary>
    /// Provides functionality to run actions with delay.
    /// </summary>
    /// <typeparam name="T">Action object type.</typeparam>
    public sealed class DelayActionRunner<T> : IDisposable
    {
        /// <summary>
        /// The event occurs before action run.
        /// </summary>
        public event EventHandler BeforeRun;

        /// <summary>
        /// The event occurs after action run.
        /// </summary>
        public event EventHandler AfterRun;

        private readonly ConcurrentDictionary<T, DateTime> updateItems =
            new ConcurrentDictionary<T, DateTime>();
        private Timer timer;
        private readonly Func<T, Task> action;
        private readonly TimeSpan delay;
        private readonly TimeSpan failRetry;
        private readonly TimeSpan tickDelay = TimeSpan.FromSeconds(3.0);
        private bool inProgress;

        private static readonly ILogger<DelayActionRunner<T>> logger = AppLogger.Create<DelayActionRunner<T>>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="action">Action to run.</param>
        /// <param name="delay">Delay timeout.</param>
        /// <param name="failRetry">Delay on fail.</param>
        public DelayActionRunner(Func<T, Task> action, TimeSpan delay, TimeSpan failRetry)
        {
            this.action = action;
            this.delay = delay;
            this.failRetry = failRetry;
        }

        /// <summary>
        /// Start monitoring thread.
        /// </summary>
        public void Start()
        {
            Stop();

            timer = new Timer(state =>
            {
                // Do nothing in case of empty data.
                if (inProgress)
                {
                    return;
                }

                try
                {
                    inProgress = true;
                    RunInternal();
                }
                finally
                {
                    inProgress = false;
                }
            }, null, TimeSpan.Zero, tickDelay);
        }

        private void RunInternal()
        {
            if (updateItems.IsEmpty)
            {
                return;
            }

            var now = DateTime.Now;
            foreach (KeyValuePair<T, DateTime> item in updateItems.Where(v => now >= v.Value).ToList())
            {
                updateItems.TryRemove(item.Key, out DateTime dt);
                // For some reason target date-time has been changed -
                // move back and skip.
                if (now < dt)
                {
                    updateItems.AddOrUpdate(item.Key, dt, (k, v) => v);
                    continue;
                }

                try
                {
                    BeforeRun?.Invoke(this, EventArgs.Empty);
                    action(item.Key).GetAwaiter().GetResult();
                    AfterRun?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Error running action for {item.Key}: {ex.Message}.");
                    if (failRetry != TimeSpan.Zero)
                    {
                        updateItems.AddOrUpdate(item.Key, DateTime.Now.Add(failRetry), (k, v) => v);
                    }
                }
            }
        }

        /// <summary>
        /// Schedule action item to process.
        /// </summary>
        /// <param name="item">Item to process.</param>
        public void Queue(T item)
        {
            var next = DateTime.Now.Add(delay);
            updateItems.AddOrUpdate(item, next, (k, v) => next);
        }

        /// <summary>
        /// Stop monitoring thread.
        /// </summary>
        public void Stop()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        #region Dispose

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
