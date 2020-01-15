using System;

namespace Synker.Domain
{
    /// <summary>
    /// Bundle related exception.
    /// </summary>
    [Serializable]
    public class BundleException : SettingsSyncException
    {
        /// <inheritdoc />
        public BundleException()
        {
        }

        /// <inheritdoc />
        public BundleException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public BundleException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
