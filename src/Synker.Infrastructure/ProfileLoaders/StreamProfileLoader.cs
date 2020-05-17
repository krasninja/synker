using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Synker.Domain;

namespace Synker.Infrastructure.ProfileLoaders
{
    /// <summary>
    /// Loads profile from provided stream.
    /// </summary>
    public class StreamProfileLoader : IProfileLoader
    {
        private readonly Stream[] streams;
        private int currentIndex;

        public StreamProfileLoader(params Stream[] streams)
        {
            if (streams.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(streams));
            }
            this.streams = streams;
        }

        public StreamProfileLoader(params string[] strings)
        {
            if (strings.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(strings));
            }
            this.streams = strings.Select(s => new MemoryStream(Encoding.UTF8.GetBytes(s)))
                .Cast<Stream>().ToArray();
        }

        public Task<Stream> GetNextAsync()
        {
            if (currentIndex >= streams.Length)
            {
                return null;
            }
            return Task.FromResult(streams[currentIndex++]);
        }
    }
}
