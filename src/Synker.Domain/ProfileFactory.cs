using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
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
    /// Profiles factory methods.
    /// </summary>
    public static class ProfileFactory
    {
        private const string TargetsKey = "targets";

        private static readonly List<Type> targetTypes = new List<Type>();

        private static readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private static readonly ILogger logger = AppLogger.Create(typeof(ProfileFactory));

        /// <summary>
        /// Add target type to resolve.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        public static void AddTargetType<T>() where T : ITarget
        {
            targetTypes.Add(typeof(T));
        }

        /// <summary>
        /// Add target types to resolve from assembly.
        /// </summary>
        /// <param name="assembly">Assembly.</param>
        /// <returns>Number of imported types.</returns>
        public static int AddTargetTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var types = assembly
                .GetTypes()
                .Where(p => typeof(ITarget).IsAssignableFrom(p))
                .ToList();
            targetTypes.AddRange(types);
            return types.Count;
        }

        /// <summary>
        /// Load profiles.
        /// </summary>
        /// <param name="profileLoader">Profiles loader..</param>
        /// <param name="excludePatterns">Patterns to exclude. None by default.</param>
        /// <returns>Profiles.</returns>
        public static async Task<IList<Profile>> LoadAsync(
            IProfileLoader profileLoader,
            IEnumerable<string> excludePatterns = null)
        {
            if (profileLoader == null)
            {
                throw new ArgumentNullException(nameof(profileLoader));
            }
            if (excludePatterns == null || !excludePatterns.Any())
            {
                excludePatterns = Enumerable.Empty<string>();
            }

            Stream stream = null;
            var profiles = new List<Profile>();
            var excludePatternsArray = excludePatterns.ToArray();
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
        public static IList<Profile> LoadFromStream(Stream stream)
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
            var profiles = new List<Profile>(yamlProfiles.Count());
            foreach (YamlMappingNode yamlProfile in yamlProfiles)
            {
                var profile = deserializer.Deserialize<Profile>(
                    new EventStreamParserAdapter(
                        YamlNodeToEventStreamConverter.ConvertToEventStream(yamlProfile)
                    ));
                var yamlActions = yamlProfile.Children[new YamlScalarNode(TargetsKey)];
                if (yamlActions == null)
                {
                    throw new SettingsSyncException("Profile stream does not contain targets.");
                }
                profile.AddTargets(ParseActions(yamlActions));
                FillMissedActionsIds(profile.Targets);
                ReplaceTokensForActions(profile.Targets);
                ValidateProfileAndActions(profile);
                logger.LogInformation($"Loaded profile {profile.Id} with {profile.Targets.Count} action(-s).");
                profiles.Add(profile);
            }

            return profiles;
        }

        /// <summary>
        /// Load profiles from YAML file.
        /// </summary>
        /// <param name="file">File name.</param>
        /// <returns>Profiles.</returns>
        public static IList<Profile> LoadFromFile(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            logger.LogInformation($"Start loading profiles from file {file}.");
            using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
            return LoadFromStream(fileStream);
        }

        private static IList<ITarget> ParseActions(YamlNode node)
        {
            var actions = new List<ITarget>();
            var yamlActions = node as YamlSequenceNode;
            if (yamlActions == null)
            {
                throw new SettingsSyncException("Targets must be a sequence.");
            }

            foreach (YamlMappingNode targetNode in yamlActions.Children)
            {
                var targetTypeString = targetNode.Children[new YamlScalarNode("type")] as YamlScalarNode;
                if (targetTypeString == null)
                {
                    continue;
                }

                var typeName = targetTypeString.Value.Replace("-", string.Empty) + "Target";
                var targetType = targetTypes.FirstOrDefault(tt =>
                    string.Compare(tt.Name, typeName, StringComparison.OrdinalIgnoreCase) == 0);
                if (targetType == null)
                {
                    throw new SettingsSyncException($"Cannot find action {targetTypeString}.");
                }

                var target = deserializer.Deserialize(
                    new EventStreamParserAdapter(
                        YamlNodeToEventStreamConverter.ConvertToEventStream(targetNode)
                    ),
                    targetType) as ITarget;
                if (target != null)
                {
                    actions.Add(target);
                }
            }

            return actions;
        }

        private static bool MatchAnyWildcardPattern(string pattern, IEnumerable<string> patternsExclude)
        {
            return patternsExclude
                .Select(RegexHelpers.WildCardToRegular)
                .Any(pi => Regex.IsMatch(pattern, pi, RegexOptions.IgnoreCase));
        }

        private static void FillMissedActionsIds(IList<ITarget> targets)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (string.IsNullOrEmpty(targets[i].Id))
                {
                    ((TargetBase) targets[i]).Id = i.ToString("000");
                }
            }
        }

        private static void ReplaceTokensForActions(IList<ITarget> targets)
        {
            foreach (ITarget target in targets)
            {
                ReplaceTokensForObject(target);
            }
        }

        private static void ReplaceTokensForObject(object obj)
        {
            foreach (var pi in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead || !pi.CanWrite ||
                    pi.Name == nameof(ITarget.Id) ||
                    pi.Name == nameof(Profile.Name) ||
                    pi.Name == nameof(ITarget.Enabled))
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

        private static void ValidateProfileAndActions(Profile profile)
        {
            ValidateObject(profile);
            foreach (ITarget profileTarget in profile.Targets)
            {
                ValidateObject(profileTarget);
            }
        }

        private static void ValidateObject(object obj)
        {
            var validationContext = new ValidationContext(obj, null, null);
            var validationResults = new List<ValidationResult>();
            var result = Validator.TryValidateObject(obj, validationContext, validationResults, true);
            if (!result)
            {
                throw new AggregateException(
                    validationResults.Select(ex => new ValidationException(ex.ErrorMessage)
                ));
            }
        }
    }
}
