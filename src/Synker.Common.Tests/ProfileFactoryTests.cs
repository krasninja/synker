using System.IO;
using System.Text;
using Synker.Common.Targets;
using Synker.Core;
using Xunit;

namespace Synker.Common.Tests
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
            var profile = ProfileFactory.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(profileText)));

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
  - type: files
    id: settings
    win:base-path: ${folder:ApplicationData}\FileZilla\
    linux:base-path: ~/.filezilla/
    patterns:
      - ""*.sqlite3""
      - ""*.xml""
";

            // Act
            var profile = ProfileFactory.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(profileText)));

            // Assert
            Assert.NotNull(profile);
            Assert.Equal(1, profile.Targets.Count);
            var filesTarget = profile.Targets[0] as FilesTarget;
            Assert.Equal(new[] { "*.sqlite3", "*.xml" }, filesTarget.Files);
        }
    }
}
