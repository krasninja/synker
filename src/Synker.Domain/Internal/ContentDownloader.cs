using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Synker.Domain.Internal
{
    /// <summary>
    /// The class allows to download content depends on protocol.
    /// </summary>
    internal class ContentDownloader
    {
        private static readonly Uri globalSource =
            new Uri(@"https://raw.githubusercontent.com/krasninja/synker-profiles/master");

        public Task<string> LoadAsync(string uri)
        {
            if (uri.StartsWith(@"http://", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith(@"https://", StringComparison.OrdinalIgnoreCase))
            {
                return LoadHttp(uri);
            }
            if (uri.StartsWith(@"global://", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri.Substring(9);
                if (!uri.StartsWith('/'))
                {
                    uri = '/' + uri;
                }
                return LoadHttp(globalSource + uri);
            }
            // Fallback.
            return LoadLocal(uri);
        }

        private static async Task<string> LoadHttp(string uri)
        {
            var webRequest = WebRequest.CreateHttp(uri);
            var response = webRequest.GetResponse();
            await using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            response.Close();
            return content;
        }

        private static Task<string> LoadLocal(string uri)
        {
            return Task.FromResult(File.ReadAllText(uri));
        }
    }
}
