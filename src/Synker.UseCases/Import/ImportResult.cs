using System.ComponentModel;

namespace Synker.UseCases.Import
{
    /// <summary>
    /// Import process result.
    /// </summary>
    public enum ImportResult
    {
        Success,

        [Description("The application settings are newer than last found bundle's")]
        SettingsDateNewerThanBundleDate,

        [Description("Cannot get local settings date")]
        CannotGetLocalSettingsDate
    }
}
