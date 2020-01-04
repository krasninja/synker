using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Synker.Core
{
    /// <summary>
    /// Application static logger.
    /// </summary>
    public static class AppLogger
    {
        /// <summary>
        /// Logger factory.
        /// </summary>
        public static ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();

        private static readonly object syncObj = new object();

        /// <summary>
        /// Create generic instance of logger. Thread safe.
        /// </summary>
        /// <typeparam name="T">Logger related type.</typeparam>
        /// <returns>Logger.</returns>
        public static ILogger<T> Create<T>()
        {
            lock (syncObj)
            {
                return LoggerFactory.CreateLogger<T>();
            }
        }

        /// <summary>
        /// Creates instance of logger. Thread safe.
        /// </summary>
        /// <param name="type">Related type.</param>
        /// <returns>Logger.</returns>
        public static ILogger Create(Type type)
        {
            lock (syncObj)
            {
                return LoggerFactory.CreateLogger(type);
            }
        }
    }
}
