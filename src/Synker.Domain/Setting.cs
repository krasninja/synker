using System.Collections.Generic;
using System.IO;

namespace Synker.Domain
{
    /// <summary>
    /// Settings item. Represents stream and metadata (dictionary).
    /// </summary>
    public class Setting
    {
        /// <summary>
        /// Identifies that target does not have output.
        /// </summary>
        public static readonly Setting EmptySetting = new Setting
        {
            Id = "empty"
        };

        /// <summary>
        /// Enumerable of empty settings.
        /// </summary>
        public static readonly IEnumerable<Setting> EmptySettings = new[]
        {
            EmptySetting
        };

        /// <summary>
        /// Setting stream.
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// Stream identifier. If empty it will be auto-generated.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Metadata dictionary.
        /// </summary>
        public IDictionary<string, string> Metadata { get; }
            = new Dictionary<string, string>();
    }
}
