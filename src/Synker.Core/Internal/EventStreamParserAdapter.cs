using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Synker.Core.Internal
{
    /// <remarks>
    /// Source: https://stackoverflow.com/questions/40696644/how-to-deserialize-a-yamlnode-in-yamldotnet
    /// </remarks>
    internal class EventStreamParserAdapter : IParser
    {
        private readonly IEnumerator<ParsingEvent> enumerator;

        public EventStreamParserAdapter(IEnumerable<ParsingEvent> events)
        {
            enumerator = events.GetEnumerator();
        }

        public ParsingEvent Current => enumerator.Current;

        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }
    }
}