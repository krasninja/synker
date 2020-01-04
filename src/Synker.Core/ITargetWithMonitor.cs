using System;

namespace Synker.Core
{
    /// <summary>
    /// Target that supports monitoring. It allows automatically
    /// export settings when they are changed.
    /// </summary>
    public interface ITargetWithMonitor
    {
        /// <summary>
        /// The event is executing on settings update. The external
        /// app subscribes on this event.
        /// </summary>
        event EventHandler<ITarget> OnSettingsUpdate;

        /// <summary>
        /// Start monitoring for target.
        /// </summary>
        void StartMonitor();

        /// <summary>
        /// Stop monitoring for target.
        /// </summary>
        void StopMonitor();
    }
}
