using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace Synker.Cli.Commands
{
    internal class ExportImportBase
    {
        [Required]
        [Option("-p|--profiles", "Application profiles directories.", CommandOptionType.MultipleValue)]
        public IReadOnlyList<string> Profiles { get; set; }

        [Required]
        [Option("-b|--bundles", "Directory with settings bundles.",
            CommandOptionType.SingleValue)]
        public string Bundles { get; set; }

        [Option("-pe|--profiles-exclude", "Application profiles to exclude.",
            CommandOptionType.MultipleValue)]
        public IReadOnlyList<string> ProfilesExclude { get; set; } = new List<string>();
    }
}
