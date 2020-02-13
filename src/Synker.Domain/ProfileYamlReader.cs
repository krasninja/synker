using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synker.Domain.Internal;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Synker.Domain
{
    /// <summary>
    /// Read profiles from YAML configuration.
    /// </summary>
    public class ProfileYamlReader
    {
        private const string TargetsKey = "targets";
        private const string ConditionsKey = "conditions";

        private readonly ICollection<Type> profileElementTypes;
        private readonly IProfileLoader profileLoader;

        private static readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private static readonly ILogger logger = AppLogger.Create(typeof(ProfileYamlReader));

        public ProfileYamlReader(IProfileLoader profileLoader, ICollection<Type> profileElementTypes)
        {
            this.profileElementTypes = profileElementTypes ??
                                       throw new ArgumentNullException(nameof(profileElementTypes));
            this.profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
        }

        /// <summary>
        /// Add target types to resolve from assembly.
        /// </summary>
        /// <param name="assembly">Assembly.</param>
        /// <returns>Number of imported types.</returns>
        public static ICollection<Type> GetProfileElementsTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            return assembly
                .GetTypes()
                .Where(p => p.IsClass)
                .Where(p => typeof(Target).IsAssignableFrom(p) ||
                            typeof(Condition).IsAssignableFrom(p))
                .ToList();
        }

        /// <summary>
        /// Load profiles.
        /// </summary>
        /// <param name="excludePatterns">Patterns to exclude. None by default.</param>
        /// <returns>Profiles.</returns>
        public async Task<IList<Profile>> LoadAsync(
            IEnumerable<string> excludePatterns = null)
        {
            if (profileLoader == null)
            {
                throw new ArgumentNullException(nameof(profileLoader));
            }

            Stream stream = null;
            var profiles = new List<Profile>();
            var excludePatternsArray = excludePatterns != null ? excludePatterns.ToArray() : new string[] {};
            while ((stream = (await profileLoader.GetNextAsync())) != null)
            {
                var streamProfiles = LoadFromStream(stream);
                stream.Close();
                stream.Dispose();
                var acceptedStreamProfiles = streamProfiles
                    .Where(p => !MatchAnyWildcardPattern(p.Id, excludePatternsArray));
                profiles.AddRange(acceptedStreamProfiles);
            }
            return profiles;
        }

        /// <summary>
        /// Load profiles from stream.
        /// </summary>
        /// <param name="stream">Text stream.</param>
        /// <returns>Loaded profiles.</returns>
        /// <exception cref="SettingsSyncException">Stream cannot be parsed.</exception>
        private IList<Profile> LoadFromStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var sr = new StreamReader(stream);
            var yaml = new YamlStream();
            yaml.Load(sr);

            if (yaml.Documents.Count != 1)
            {
                throw new SettingsSyncException("Incorrect profiles stream format.");
            }

            var yamlProfiles = yaml.Documents[0].RootNode is YamlSequenceNode yamlProfilesSeq ?
                yamlProfilesSeq.Children :
                new List<YamlNode> { yaml.Documents[0].RootNode };
            yamlProfiles = LoadIncludes(yamlProfiles);
            var profiles = new List<Profile>(yamlProfiles.Count);
            foreach (YamlMappingNode yamlProfile in yamlProfiles)
            {
                var profile = deserializer.Deserialize<Profile>(
                    new EventStreamParserAdapter(
                        YamlNodeToEventStreamConverter.ConvertToEventStream(yamlProfile)
                    ));

                // Parse targets.
                var yamlTargets = yamlProfile.Children[new YamlScalarNode(TargetsKey)] as YamlSequenceNode;
                if (yamlTargets == null)
                {
                    throw new SettingsSyncException("Profile stream does not contain targets.");
                }
                foreach (YamlMappingNode yamlTargetNode in yamlTargets.Children)
                {
                    var target = ParseElement<Target>(yamlTargetNode, "Target");
                    target = profile.AddTarget(target);

                    // Parse conditions.
                    var yamlConditions = yamlTargetNode.Children[new YamlScalarNode(ConditionsKey)] as YamlSequenceNode;
                    if (yamlConditions != null)
                    {
                        foreach (YamlMappingNode yamlCondition in yamlConditions)
                        {
                            var condition = ParseElement<Condition>(yamlCondition, typePostfix: "Condition");
                            target.AddCondition(condition);
                        }
                    }
                }

                // Validate profile.
                var profileValidationResults = Saritasa.Tools.Domain.ValidationErrors.CreateFromObjectValidation(profile);
                if (profileValidationResults.HasErrors)
                {
                    throw new Saritasa.Tools.Domain.Exceptions.ValidationException(profileValidationResults);
                }

                logger.LogInformation($"Loaded profile {profile.Id} with {profile.Targets.Count} target(-s).");
                profiles.Add(profile);
            }

            return profiles;
        }

        /// <summary>
        /// Load profiles from YAML file.
        /// </summary>
        /// <param name="file">File name.</param>
        /// <returns>Profiles.</returns>
        public IList<Profile> LoadFromFile(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            logger.LogInformation($"Start loading profiles from file {file}.");
            using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
            return LoadFromStream(fileStream);
        }

        private IList<YamlNode> LoadIncludes(IList<YamlNode> yamlNodes)
        {
            var list = new List<YamlNode>();
            foreach (YamlNode yamlNode in yamlNodes)
            {
                if (yamlNode is YamlScalarNode yamlScalarNode && yamlScalarNode.Tag == "!include:")
                {
                    var contentDownloader = new ContentDownloader();
                    var content = contentDownloader.LoadAsync(yamlScalarNode.Value).GetAwaiter().GetResult();
                    var yaml = new YamlStream();
                    yaml.Load(new StringReader(content));
                    var yamlProfiles = yaml.Documents[0].RootNode is YamlSequenceNode yamlProfilesSeq ?
                        yamlProfilesSeq.Children :
                        new List<YamlNode> { yaml.Documents[0].RootNode };
                    foreach (YamlNode allNode in yamlProfiles)
                    {
                        list.Add(allNode);
                    }
                }
                else
                {
                    list.Add(yamlNode);
                }
            }
            return list;
        }

        private T ParseElement<T>(YamlMappingNode yamlElementNode, string typePostfix = null) where T : class
        {
            var yamlType = yamlElementNode.Children[new YamlScalarNode("type")] as YamlScalarNode;
            if (yamlType == null)
            {
                throw new SettingsSyncException("Element does not contain \"type\"");
            }

            var typeName = yamlType.Value.Replace("-", string.Empty) + (typePostfix ?? string.Empty);
            var elementType = profileElementTypes.FirstOrDefault(t =>
                string.Compare(t.Name, typeName, StringComparison.OrdinalIgnoreCase) == 0);
            if (elementType == null)
            {
                throw new SettingsSyncException($"Cannot find target {yamlType}.");
            }

            var element = deserializer.Deserialize(
                new EventStreamParserAdapter(
                    YamlNodeToEventStreamConverter.ConvertToEventStream(yamlElementNode)
                ),
                elementType) as T;
            if (element == null)
            {
                throw new SettingsSyncException($"Cannot deserialize {yamlType}.");
            }
            ReplaceTokensForObject(element);
            return element;
        }

        private static bool MatchAnyWildcardPattern(string pattern, IEnumerable<string> patternsExclude)
        {
            return patternsExclude
                .Select(RegexHelpers.WildCardToRegular)
                .Any(pi => Regex.IsMatch(pattern, pi, RegexOptions.IgnoreCase));
        }

        private static void ReplaceTokensForObject(object obj)
        {
            foreach (var pi in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite ||
                    pi.Name == nameof(Target.Id) ||
                    pi.Name == nameof(Target.Conditions))
                {
                    continue;
                }

                if (pi.PropertyType == typeof(string))
                {
                    pi.SetValue(obj, TemplateString.ReplaceTokens((string) pi.GetValue(obj)));
                    continue;
                }

                if (pi.GetValue(obj) is IList listObj)
                {
                    for (var i = 0; i < listObj.Count; i++)
                    {
                        var eObj = listObj[i];
                        if (eObj is string)
                        {
                            listObj[i] = TemplateString.ReplaceTokens(eObj.ToString());
                        }
                        else
                        {
                            ReplaceTokensForObject(eObj);
                        }
                    }
                }
            }
        }
    }
}
