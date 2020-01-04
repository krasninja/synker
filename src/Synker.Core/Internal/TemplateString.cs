using System;
using System.Text;

namespace Synker.Core.Internal
{
    /// <summary>
    /// Class is to process template strings. For example "This is ${env:VALUE}.".
    /// </summary>
    public static class TemplateString
    {
        public static string ReplaceTokens(string str)
        {
            if (str == null)
            {
                return string.Empty;
            }
            if (str.Length < 3)
            {
                return str;
            }
            var sb = new StringBuilder(str.Length);
            sb.Append(str[0]);
            bool inTemplate = false;
            int tokenStartInd = -1;
            for (int i = 1; i < str.Length; i++)
            {
                if (!inTemplate && str[i] == '{' && str[i - 1] == '$' && str.Length > i + 1)
                {
                    tokenStartInd = i + 1;
                    inTemplate = true;
                    sb.Remove(sb.Length - 1, 1);
                }
                else if (inTemplate && str[i] == '}')
                {
                    sb.Append(
                        ProcessTokenString(str.Substring(tokenStartInd, i - tokenStartInd)));
                    inTemplate = false;
                    tokenStartInd = -1;
                }
                else if (!inTemplate)
                {
                    sb.Append(str[i]);
                }
            }
            return sb.ToString();
        }

        private static string ProcessTokenString(string str)
        {
            var arr = str.Trim().ToLowerInvariant().Split(new [] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length < 2)
            {
                return string.Empty;
            }

            switch (arr[0].Trim())
            {
                case "folder":
                    var ret = Enum.TryParse(arr[1], true, out Environment.SpecialFolder folder);
                    if (!ret)
                    {
                        throw new SettingsSyncException($"Cannot get special folder {arr[1]}.");
                    }
                    return Environment.GetFolderPath(folder);
                case "env":
                    return Environment.GetEnvironmentVariable(arr[1], EnvironmentVariableTarget.User);
            }
            return string.Empty;
        }
    }
}
