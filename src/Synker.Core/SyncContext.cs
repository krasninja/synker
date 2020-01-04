using System.Collections.Generic;

namespace Synker.Core
{
    /// <summary>
    /// Synchronization context.
    /// </summary>
    public class SyncContext
    {
        /// <summary>
        /// Dictionary to exchange data between targets.
        /// </summary>
        public IDictionary<string, string> Items { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Do not continue export or import if true.
        /// </summary>
        public bool CancelProcessing { get; set; }
    }
}
