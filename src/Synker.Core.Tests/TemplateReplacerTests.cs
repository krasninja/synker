using Synker.Core.Internal;
using Xunit;

namespace Synker.Core.Tests
{
    /// <summary>
    /// Template replacer tests.
    /// </summary>
    public class TemplateReplacerTests
    {
        [Fact]
        public void ReplaceTokens_StringWithSpecialFolder_ResolvedString()
        {
            // Arrange
            var path = @"${folder:Windows}\Code\User\";

            // Act
            var result = TemplateString.ReplaceTokens(path);

            // Assert
            Assert.Equal(@"C:\Windows\Code\User\", result, ignoreCase: true);
        }
    }
}
