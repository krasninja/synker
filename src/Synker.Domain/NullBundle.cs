using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synker.Domain
{
    /// <summary>
    /// Useless bundle, for testing only.
    /// </summary>
    public class NullBundle : IBundle
    {
        /// <inheritdoc />
        public string Id { get; } = "null";

        /// <inheritdoc />
        public Task<string> PutSettingAsync(Setting setting, string targetId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Guid.NewGuid().ToString("N"));
        }

        /// <inheritdoc />
        public Task PutMetadataAsync(IDictionary<string, string> metadata, string targetId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Setting> GetSettingsAsync(string targetId)
        {
            yield return null;
        }

        /// <inheritdoc />
        public Task<IDictionary<string, string>> GetMetadataAsync(string targetId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDictionary<string, string>>(
                new Dictionary<string, string>());
        }
    }
}
