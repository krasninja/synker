using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synker.Domain;

namespace Synker.Infrastructure.ProfileLoaders
{
    /// <summary>
    /// Load profiles from files or directory.
    /// </summary>
    public class FilesProfileLoader : IProfileLoader
    {
        private readonly IEnumerable<string> sources;
        private IList<string> files;
        private int currentIndex;
        private readonly ILogger<FilesProfileLoader> logger = AppLogger.Create<FilesProfileLoader>();

        public FilesProfileLoader(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException(nameof(sources));
            }
            this.sources = GetFieldsFromLine(source).ToList();
        }

        public FilesProfileLoader(IEnumerable<string> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }
            this.sources = sources;
        }

        /// <inheritdoc />
        public Task<Stream> GetNextAsync()
        {
            if (files == null)
            {
                Init();
            }

            if (currentIndex + 1 > files.Count)
            {
                return Task.FromResult<Stream>(null);
            }
            logger.LogTrace($"Loading profile from file {files[currentIndex]} .");
            var stream = File.Open(files[currentIndex], FileMode.Open, FileAccess.Read, FileShare.None);
            currentIndex++;
            return Task.FromResult<Stream>(stream);
        }

        private void Init()
        {
            files = new List<string>();
            foreach (string source in sources)
            {
                var basePath = source;
                var fileNamePattern = "*.yaml";

                if (!Directory.Exists(source))
                {
                    if (!File.Exists(source))
                    {
                        throw new SettingsSyncException($"Directory or file \"{source}\" does not exist.");
                    }
                    fileNamePattern = Path.GetFileName(source);
                    basePath = Path.GetDirectoryName(source);
                }

                foreach (string dirFile in Directory.EnumerateFiles(basePath, fileNamePattern))
                {
                    // Skip "hidden" files.
                    if (Path.GetFileName(dirFile).StartsWith("."))
                    {
                        continue;
                    }

                    if (string.CompareOrdinal(Path.GetExtension(dirFile), ".yaml") != 0)
                    {
                        continue;
                    }

                    files.Add(dirFile);
                }
            }
        }

        /// <remarks>
        /// See source at https://www.codeproject.com/Tips/823670/Csharp-Light-and-Fast-CSV-Parser .
        /// </remarks>
        private static string[] GetFieldsFromLine(string target, char delimiter = ',')
        {
            var inQuote = false;
            var records = new List<string>();
            var sb = new StringBuilder();
            var reader = new StringReader(target);

            while (reader.Peek() != -1)
            {
                var readChar = (char)reader.Read();

                if (readChar == '\n' || (readChar == '\r' && (char)reader.Peek() == '\n'))
                {
                    // If it's a \r\n combo consume the \n part and throw it away.
                    if (readChar == '\r')
                    {
                        reader.Read();
                    }

                    if (inQuote)
                    {
                        if (readChar == '\r')
                        {
                            sb.Append('\r');
                        }
                        sb.Append('\n');
                    }
                    else
                    {
                        if (records.Count > 0 || sb.Length > 0)
                        {
                            records.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                }
                else if (sb.Length == 0 && !inQuote)
                {
                    if (readChar == '"')
                    {
                        inQuote = true;
                    }
                    else if (readChar == delimiter)
                    {
                        records.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (char.IsWhiteSpace(readChar))
                    {
                        // Ignore leading whitespace.
                    }
                    else
                    {
                        sb.Append(readChar);
                    }
                }
                else if (readChar == delimiter)
                {
                    if (inQuote)
                    {
                        sb.Append(delimiter);
                    }
                    else
                    {
                        records.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else if (readChar == '"')
                {
                    if (inQuote)
                    {
                        if ((char)reader.Peek() == '"')
                        {
                            reader.Read();
                            sb.Append('"');
                        }
                        else
                        {
                            inQuote = false;
                        }
                    }
                    else
                    {
                        sb.Append(readChar);
                    }
                }
                else
                {
                    sb.Append(readChar);
                }
            }

            if (records.Count > 0 || sb.Length > 0)
            {
                records.Add(sb.ToString());
            }

            return records.ToArray();
        }
    }
}
