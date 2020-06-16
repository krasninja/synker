using System.Collections.Generic;
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
        [Option("-p|--profiles", "Application profiles directories.", CommandOptionType.MultipleValue)]
        public IReadOnlyList<string> Profiles { get; set; } = new string[] {};

        [Option("-b|--bundles", "Directory with settings bundles.",
            CommandOptionType.SingleValue)]
        public string Bundles { get; set; } = string.Empty;

        [Option("-pe|--profiles-exclude", "Application profiles to exclude.",
            CommandOptionType.MultipleValue)]
        public IReadOnlyList<string> ProfilesExclude { get; set; } = new string[] {};

        [Option("-c|--config", "Configuration file.", CommandOptionType.SingleValue)]
        public string Config { get; set; }

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
            return await profileYamlReader.LoadAsync();
        }
    }
}
