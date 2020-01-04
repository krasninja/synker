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
using Synker.Core.Internal;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Synker.Core
{
    /// <summary>
    /// Profiles factory methods.
    /// </summary>
    public static class ProfileFactory
    {
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
                var profile = LoadFromStream(stream);
                stream.Close();
                stream.Dispose();
                if (MatchAnyWildcardPattern(profile.Id, excludePatternsArray))
                {
                    continue;
                }
                profiles.Add(profile);
            }
            return profiles;
        }

        /// <summary>
        /// Load profile from stream.
        /// </summary>
        /// <param name="stream">Text stream.</param>
        /// <returns>Profile.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Profile LoadFromStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var sr = new StreamReader(stream);
            var yaml = new YamlStream();
            yaml.Load(sr);

            var profile = deserializer.Deserialize<Profile>(
                new EventStreamParserAdapter(
                    YamlNodeToEventStreamConverter.ConvertToEventStream(yaml.Documents[0])
                ));
            var targetsNode = ((YamlMappingNode)yaml.Documents[0].RootNode)
                .Children[new YamlScalarNode("targets")];
            if (targetsNode == null)
            {
                throw new SettingsSyncException("Profile file does not contain targets.");
            }
            profile.AddTargets(ParseTargets(targetsNode));
            FillMissedTargetsIds(profile.Targets);
            ReplaceTokensForTargets(profile.Targets);
            ValidateProfileAndTargets(profile);
            logger.LogInformation($"Loaded profile {profile.Id} with {profile.Targets.Count} target(-s).");
            return profile;
        }

        /// <summary>
        /// Load profile from YAML file.
        /// </summary>
        /// <param name="file">File name.</param>
        /// <returns>Profile.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Profile LoadFromFile(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            logger.LogInformation($"Start loading profile from file {file}.");
            using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
            return LoadFromStream(fileStream);
        }

        private static IList<ITarget> ParseTargets(YamlNode node)
        {
            var targets = new List<ITarget>();
            var sequenceNode = node as YamlSequenceNode;
            if (sequenceNode == null)
            {
                throw new SettingsSyncException("Targets must be sequence.");
            }

            foreach (YamlMappingNode targetNode in sequenceNode.Children)
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
                    throw new SettingsSyncException($"Cannot find target {targetTypeString}.");
                }

                var target = deserializer.Deserialize(
                    new EventStreamParserAdapter(
                        YamlNodeToEventStreamConverter.ConvertToEventStream(targetNode)
                    ),
                    targetType) as ITarget;
                if (target != null)
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        private static bool MatchAnyWildcardPattern(string pattern, IEnumerable<string> patternsExclude)
        {
            return patternsExclude
                .Select(RegexHelpers.WildCardToRegular)
                .Any(pi => Regex.IsMatch(pattern, pi, RegexOptions.IgnoreCase));
        }

        private static void FillMissedTargetsIds(IList<ITarget> targets)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (string.IsNullOrEmpty(targets[i].Id))
                {
                    ((TargetBase) targets[i]).Id = i.ToString("000");
                }
            }
        }

        private static void ReplaceTokensForTargets(IList<ITarget> targets)
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

        private static void ValidateProfileAndTargets(Profile profile)
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
