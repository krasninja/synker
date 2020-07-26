using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Saritasa.Tools.Domain.Exceptions;
using Synker.Domain;
using Synker.Infrastructure.ProfileLoaders;
using Synker.Infrastructure.Targets;

namespace Synker.Cli.Commands
{
    internal class ExportImportCommand : AppCommand
    {
        [Option("-p|--profiles", "Application profiles directories or YAML file.", CommandOptionType.MultipleValue)]
        public IReadOnlyCollection<string> Profiles { get; set; } = new string[] {};

        [Option("-b|--bundles", "Directory with settings bundles.",
            CommandOptionType.SingleValue)]
        public string Bundles { get; set; } = string.Empty;

        [Option("-op|--only-profiles", "Restrict command to only specified profiles names.",
            CommandOptionType.MultipleValue)]
        public IReadOnlyCollection<string> OnlyProfiles { get; set; } = new string[] {};

        [Option("-c|--config", "Configuration file.", CommandOptionType.SingleValue)]
        public string Config { get; set; }

        [Option("-f|--force", "Force export or import.", CommandOptionType.NoValue)]
        public bool Force { get; set; } = false;

        protected UserConfiguration GetUserConfiguration()
        {
            UserConfiguration userConfiguration = null;
            if (!Profiles.Any() && string.IsNullOrEmpty(Bundles))
            {
                userConfiguration = UserConfiguration.LoadFromFile(Config);
            }

            if (Profiles.Any())
            {
                userConfiguration = UserConfiguration.CreateEmpty(dict =>
                {
                    dict[UserConfiguration.ProfilesSourceKey] = string.Join(';', Profiles);
                    dict[UserConfiguration.BundlesDirectoryKey] = Bundles;
                });
            }

            if (userConfiguration == null)
            {
                throw new DomainException("Invalid configuration.");
            }

            userConfiguration.ApplyDefaults();
            return userConfiguration;
        }

        protected async Task<IList<Profile>> GetProfilesAsync(UserConfiguration config)
        {
            var filesProfileLoader = new FilesProfileLoader(config.ProfilesSource);
            var profileYamlReader = new ProfileYamlReader(filesProfileLoader,
                ProfileYamlReader.GetProfileElementsTypesFromAssembly(typeof(NullSettingsTarget).Assembly));
            var profiles = await profileYamlReader.LoadAsync();
            if (OnlyProfiles.Any())
            {
                var notFoundProfiles = OnlyProfiles.Where(op => !profiles.Select(p => p.Id).Contains(op)).ToArray();
                if (notFoundProfiles.Any())
                {
                    throw new DomainException($"Cannot find profile(-s) {string.Join(", ", notFoundProfiles)}.");
                }
                profiles = profiles.Where(p => OnlyProfiles.Contains(p.Id)).ToList();
            }
            return profiles;
        }
    }
}
