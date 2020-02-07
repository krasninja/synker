using System.Linq;
using System.Threading.Tasks;
using Synker.Infrastructure.Targets;
using Synker.Domain;
using Synker.Infrastructure.ProfileLoaders;
using Xunit;

namespace Synker.Infrastructure.Tests
{
    /// <summary>
    /// Profile factory creation tests.
    /// </summary>
    public class ProfileFactoryTests
    {
        [Fact]
        public async Task LoadFromStream_NullTargetProfile_ParseProfile()
        {
            // Arrange
            var profileText = @"
id: test
name: Test
description: test test
targets:
  - type: null
    id: main
";

            // Act
            var profileYamlReader = new ProfileYamlReader(new StreamProfileLoader(profileText),
                ProfileYamlReader.GetProfileElementsTypesFromAssembly(typeof(NullSettingsTarget).Assembly));
            var profile = (await profileYamlReader.LoadAsync()).First();

            // Assert
            Assert.NotNull(profile);
            Assert.Equal("test", profile.Id);
            Assert.Equal("Test", profile.Name);
            Assert.Equal("test test", profile.Description);
            Assert.Single(profile.Targets);
            Assert.IsType<NullSettingsTarget>(profile.Targets[0]);
            Assert.Equal("main", profile.Targets[0].Id);
        }

        [Fact]
        public async Task LoadFromStream_FilesTargetProfileWith_ParseProfilePatternsCorrect()
        {
            // Arrange
            var profileText = @"
id: filezilla
name: FileZilla
description: FileZilla layout and hosts settings.
targets:
  - type: add-files-to-bundle
    id: settings
    win:base-path: ${folder:ApplicationData}\FileZilla\
    linux:base-path: ~/.filezilla/
    files:
      - ""*.sqlite3""
      - ""*.xml""
";

            // Act
            var profileYamlReader = new ProfileYamlReader(new StreamProfileLoader(profileText),
                ProfileYamlReader.GetProfileElementsTypesFromAssembly(typeof(NullSettingsTarget).Assembly));
            var profile = (await profileYamlReader.LoadAsync()).First();

            // Assert
            Assert.NotNull(profile);
            Assert.Single(profile.Targets);
            var filesTarget = profile.Targets[0] as AddFilesToBundleTarget;
            Assert.Equal(new[] { "*.sqlite3", "*.xml" }, filesTarget.Files);
        }
    }
}
