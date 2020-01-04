using System;

namespace Synker.Core
{
    /// <summary>
    /// Settings Synker related exception.
    /// </summary>
    [Serializable]
    public class SettingsSyncException : Exception
    {
        /// <inheritdoc />
        public SettingsSyncException()
        {
        }

        /// <inheritdoc />
        public SettingsSyncException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public SettingsSyncException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
