using System.IO;
using System.Threading.Tasks;

namespace Synker.Domain
{
    /// <summary>
    /// Profiles loader.
    /// </summary>
    public interface IProfileLoader
    {
        /// <summary>
        /// Get next profile stream. Null indicates end of content.
        /// </summary>
        /// <returns>Text stream.</returns>
        Task<Stream> GetNextAsync();
    }
}
