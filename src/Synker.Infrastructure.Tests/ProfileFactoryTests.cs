using System.IO;
using System.Linq;
using System.Text;
using Synker.Infrastructure.Targets;
using Synker.Domain;
using Xunit;

namespace Synker.Infrastructure.Tests
{
    /// <summary>
    /// Profile factory creation tests.
    /// </summary>
    public class ProfileFactoryTests
    {
        [Fact]
        public void LoadFromStream_NullTargetProfile_ParseProfile()
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
            ProfileFactory.AddTargetTypesFromAssembly(typeof(NullTarget).Assembly);
            var profile = ProfileFactory.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(profileText))).First();

            // Assert
            Assert.NotNull(profile);
            Assert.Equal("test", profile.Id);
            Assert.Equal("Test", profile.Name);
            Assert.Equal("test test", profile.Description);
            Assert.Equal(1, profile.Targets.Count);
            Assert.IsType<NullTarget>(profile.Targets[0]);
            Assert.Equal("main", profile.Targets[0].Id);
        }

        [Fact]
        public void LoadFromStream_FilesTargetProfileWith_ParseProfilePatternsCorrect()
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
            ProfileFactory.AddTargetTypesFromAssembly(typeof(NullTarget).Assembly);
            var profile = ProfileFactory.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(profileText))).First();

            // Assert
            Assert.NotNull(profile);
            Assert.Equal(1, profile.Targets.Count);
            var filesTarget = profile.Targets[0] as AddFilesToBundleTarget;
            Assert.Equal(new[] { "*.sqlite3", "*.xml" }, filesTarget.Files);
        }
    }
}
