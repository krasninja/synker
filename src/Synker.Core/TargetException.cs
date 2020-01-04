using System;

namespace Synker.Core
{
    /// <summary>
    /// Target related exception.
    /// </summary>
    [Serializable]
    public class TargetException : SettingsSyncException
    {
        /// <inheritdoc />
        public TargetException()
        {
        }

        /// <inheritdoc />
        public TargetException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public TargetException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
