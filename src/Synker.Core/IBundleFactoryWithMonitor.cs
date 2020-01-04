using System;

namespace Synker.Core
{
    /// <summary>
    /// Bundle factory that can monitor new settings.
    /// </summary>
    public interface IBundleFactoryWithMonitor
    {
        /// <summary>
        /// The event is executing on receiving new settings.
        /// </summary>
        event EventHandler<string> OnSettingsUpdate;

        /// <summary>
        /// Start monitoring for new settings.
        /// </summary>
        void StartMonitor();

        /// <summary>
        /// Stop monitoring for new settings.
        /// </summary>
        void StopMonitor();
    }
}
