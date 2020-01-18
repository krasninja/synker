using Synker.Infrastructure.Targets;
using Xunit;

namespace Synker.Infrastructure.Tests
{
    /// <summary>
    /// Tests for <see cref="AddFilesToBundleTarget" />.
    /// </summary>
    public class AddFilesToBundleActionTests
    {
        [Theory]
        [InlineData(@"C:\work\synker\", @"c:\Work\synker\file.txt", true, @".\file.txt")]
        [InlineData(@"C:\work\synker\", @"c:\Work\synker\subdir\file.txt", true, @".\subdir\file.txt")]
        [InlineData(@"C:\work\synker", @"C:\work\synker\file.txt", true, @".\file.txt")]
        [InlineData(@"C:/work/synker", @"C:/work/synker/file.txt", true, @"./file.txt")]
        [InlineData(@"/home/user/.data/", @"/home/user/.data/subdir/file.txt", false, @"./subdir/file.txt")]
        [InlineData(@"/home/user/.data", @"/home/user/.data/subdir/file.txt", false, @"./subdir/file.txt")]
        public void GetRelativePath_BasePathWithRelative_CorrectRelativePath(string relativeTo, string path,
            bool caseInsensitive, string expected)
        {
            // Arrange & act
            var relativePath = AddFilesToBundleTarget.GetRelativeFilePath(relativeTo, path, caseInsensitive);

            // Assert
            Assert.Equal(expected, relativePath);
        }
    }
}
