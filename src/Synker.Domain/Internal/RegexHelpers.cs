using System.Text.RegularExpressions;

namespace Synker.Domain.Internal
{
    /// <summary>
    /// Regex helpers.
    /// </summary>
    internal static class RegexHelpers
    {
        internal static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value)
                       .Replace("\\?", ".")
                       .Replace("\\*", ".*") + "$";
        }
    }
}
