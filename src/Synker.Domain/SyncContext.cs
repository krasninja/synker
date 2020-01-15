using System.Collections.Generic;

namespace Synker.Domain
{
    /// <summary>
    /// Synchronization context.
    /// </summary>
    public class SyncContext
    {
        /// <summary>
        /// Dictionary to exchange data between steps.
        /// </summary>
        public IDictionary<string, string> Items { get; } = new Dictionary<string, string>();

        /// <summary>
        /// If step sets this property to <c>True</c>, the import/export process will be cancelled
        /// and further steps will be ignored.
        /// </summary>
        public bool CancelProcessing { get; set; }
    }
}
