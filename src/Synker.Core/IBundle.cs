using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synker.Core
{
    /// <summary>
    /// Bundle class arranges settings objects and provides unified access to
    /// settings and metadata.
    /// </summary>
    public interface IBundle
    {
        /// <summary>
        /// Bundle identifier.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Put setting to target.
        /// </summary>
        /// <param name="setting">Setting.</param>
        /// <param name="targetId">Target identifier.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Setting identifier.</returns>
        Task<string> PutSettingAsync(Setting setting, string targetId, CancellationToken cancellationToken =
            default);

        /// <summary>
        /// Put metadata. If targetId is null or empty metadata will be related to bundle.
        /// If targetId is not null metadata will be related to target.
        /// </summary>
        /// <param name="metadata">Metadata dictionary.</param>
        /// <param name="targetId">Target identifier or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Task.</returns>
        Task PutMetadataAsync(IDictionary<string, string> metadata, string targetId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get settings related to target.
        /// </summary>
        /// <param name="targetId">Target identifier.</param>
        /// <returns>Settings enumeration.</returns>
        IAsyncEnumerable<Setting> GetSettingsAsync(string targetId);

        /// <summary>
        /// Get metadata. If targetId is null or empty metadata will be related to bundle.
        /// If targetId is not null metadata will be related to target.
        /// </summary>
        /// <param name="targetId">Target identifier or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>Metadata dictionary.</returns>
        Task<IDictionary<string, string>> GetMetadataAsync(string targetId = null,
            CancellationToken cancellationToken = default);
    }
}
