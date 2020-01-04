using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Synker.Core.Internal
{
    /// <summary>
    /// Platform specific tags replacer.
    /// </summary>
    internal static class PlatformSpecificTagsReplacer
    {
        /// <summary>
        /// Replaces string like "win:path" to "path" if current platform is Windows.
        /// If platform does not match returns empty string.
        /// </summary>
        /// <param name="tag">Tag to resolve</param>
        /// <param name="resolvedPlatforms">Dictionary of resolve tag to make sure we always follow much
        /// specific platform.</param>
        /// <returns>Resolved tag or null.</returns>
        public static string GetPlatformResolvedTag(string tag, ISet<string> resolvedPlatforms)
        {
            var ind = tag.IndexOf(":", StringComparison.Ordinal);
            if (ind < 0)
            {
                return tag;
            }
            var platform = tag.Substring(0, ind).ToLowerInvariant().Trim();
            var name = tag.Substring(ind + 1, tag.Length - ind - 1).ToLowerInvariant().Trim();

            // Windows.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (platform == Platforms.Windows &&
                    !HasFormatPlatformTagId(resolvedPlatforms, Platforms.WindowsX86, name) &&
                    !HasFormatPlatformTagId(resolvedPlatforms, Platforms.WindowsX64, name))
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.Windows, name));
                    return name;
                }
                if (platform == Platforms.WindowsX86 && RuntimeInformation.OSArchitecture == Architecture.X86)
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.WindowsX86, name));
                    return name;
                }
                if (platform == Platforms.WindowsX64 && RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.WindowsX64, name));
                    return name;
                }
                if (platform == Platforms.WindowsArm && RuntimeInformation.OSArchitecture == Architecture.Arm)
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.WindowsArm, name));
                    return name;
                }
                if (platform == Platforms.WindowsArmX64 && RuntimeInformation.OSArchitecture == Architecture.Arm64)
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.WindowsArmX64, name));
                    return name;
                }
            }

            // Linux.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (platform == Platforms.Linux &&
                    !HasFormatPlatformTagId(resolvedPlatforms, Platforms.LinuxX86, name) &&
                    !HasFormatPlatformTagId(resolvedPlatforms, Platforms.LinuxX64, name))
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.Windows, name));
                    return name;
                }
                if (platform == Platforms.LinuxX86 && RuntimeInformation.OSArchitecture == Architecture.X86)
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.LinuxX86, name));
                    return name;
                }
                if (platform == Platforms.LinuxX64 && RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.LinuxX64, name));
                    return name;
                }
                if (platform == Platforms.LinuxArm && RuntimeInformation.OSArchitecture == Architecture.Arm)
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.LinuxArm, name));
                    return name;
                }
                if (platform == Platforms.LinuxArmX64 && RuntimeInformation.OSArchitecture == Architecture.Arm64)
                {
                    resolvedPlatforms.Add(FormatPlatformTagId(Platforms.LinuxArmX64, name));
                    return name;
                }
            }

            // macOS.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                resolvedPlatforms.Add(FormatPlatformTagId(Platforms.Osx, name));
                return name;
            }

            return string.Empty;
        }

        private static string FormatPlatformTagId(string platform, string tag) => $"{platform}:{tag}";

        private static bool HasFormatPlatformTagId(ISet<string> set, string platform, string tag)
            => set.Contains(FormatPlatformTagId(platform, tag));
    }
}
