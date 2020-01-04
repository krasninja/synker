using System;
using System.Collections.Generic;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Synker.Core.Internal
{
    /// <remarks>
    /// Source: https://stackoverflow.com/questions/40696644/how-to-deserialize-a-yamlnode-in-yamldotnet
    /// </remarks>
    internal static class YamlNodeToEventStreamConverter
    {
        public static IEnumerable<ParsingEvent> ConvertToEventStream(YamlStream stream)
        {
            yield return new StreamStart();
            foreach (var document in stream.Documents)
            {
                foreach (var evt in ConvertToEventStream(document))
                {
                    yield return evt;
                }
            }
            yield return new StreamEnd();
        }

        public static IEnumerable<ParsingEvent> ConvertToEventStream(YamlDocument document)
        {
            yield return new DocumentStart();
            foreach (var evt in ConvertToEventStream(document.RootNode))
            {
                yield return evt;
            }
            yield return new DocumentEnd(false);
        }

        public static IEnumerable<ParsingEvent> ConvertToEventStream(YamlNode node)
        {
            if (node is YamlScalarNode scalar)
            {
                return ConvertToEventStream(scalar);
            }

            if (node is YamlSequenceNode sequence)
            {
                return ConvertToEventStream(sequence);
            }

            if (node is YamlMappingNode mapping)
            {
                return ConvertToEventStream(mapping);
            }

            throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}.");
        }

        private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlScalarNode scalar)
        {
            yield return new Scalar(scalar.Anchor, scalar.Tag, scalar.Value, scalar.Style, false, false);
        }

        private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlSequenceNode sequence)
        {
            yield return new SequenceStart(sequence.Anchor, sequence.Tag, false, sequence.Style);
            foreach (var node in sequence.Children)
            {
                foreach (var evt in ConvertToEventStream(node))
                {
                    yield return evt;
                }
            }
            yield return new SequenceEnd();
        }

        private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlMappingNode mapping)
        {
            yield return new MappingStart(mapping.Anchor, mapping.Tag, false, mapping.Style);
            ISet<string> resolvedPlatformTags = new HashSet<string>();
            foreach (var pair in mapping.Children)
            {
                foreach (var evt in ConvertToEventStream(pair.Key))
                {
                    ReplacePlatformSpecificToNodeAndVerify(pair.Key, resolvedPlatformTags);
                    yield return evt;
                }
                foreach (var evt in ConvertToEventStream(pair.Value))
                {
                    yield return evt;
                }
            }
            yield return new MappingEnd();
        }

        private static YamlNode ReplacePlatformSpecificToNodeAndVerify(YamlNode node, ISet<string> resolvedPlatformTags)
        {
            var scalarNode = node as YamlScalarNode;
            if (scalarNode != null)
            {
                var name = PlatformSpecificTagsReplacer.GetPlatformResolvedTag(scalarNode.Value, resolvedPlatformTags);
                if (!string.IsNullOrEmpty(name))
                {
                    scalarNode.Value = name;
                }
                else
                {
                    // In case the platform does not match - make it like comment to skip.
                    scalarNode.Value = "#" + name;
                }
            }
            return node;
        }
    }
}
