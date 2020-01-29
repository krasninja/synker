using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Saritasa.Tools.Domain.Exceptions;
using Synker.Domain;
using Synker.Infrastructure.ProfileLoaders;

namespace Synker.Cli.Commands
{
    internal class ExportImportBase
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

        public UserConfiguration GetUserConfiguration()
        {
            if (!Profiles.Any() && string.IsNullOrEmpty(Bundles))
            {
                return UserConfiguration.LoadFromFile(Config);
            }

            if (Profiles.Any() && !string.IsNullOrWhiteSpace(Bundles))
            {
                return UserConfiguration.CreateEmpty(dict =>
                {
                    dict[UserConfiguration.ProfilesSourceKey] = string.Join(';', Profiles);
                    dict[UserConfiguration.BundlesDirectoryKey] = Bundles;
                });
            }

            throw new DomainException("Invalid configuration.");
        }

        protected async Task<IList<Profile>> GetProfilesAsync(UserConfiguration config)
        {
            var filesProfileLoader = new FilesProfileLoader(config.ProfilesSource);
            return await ProfileFactory.LoadAsync(filesProfileLoader, ProfilesExclude);
        }
    }
}
