using System;
using Saritasa.Tools.Domain.Exceptions;

namespace Synker.Domain
{
    /// <summary>
    /// Settings Synker related exception.
    /// </summary>
    [Serializable]
    public class SettingsSyncException : DomainException
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
