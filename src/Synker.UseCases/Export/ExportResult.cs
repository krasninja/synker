using System.ComponentModel;

namespace Synker.UseCases.Export
{
    /// <summary>
    /// Export process result.
    /// </summary>
    public enum ExportResult
    {
        Success,

        [Description("The application settings are older than last found bundle's")]
        SettingsDateOlderThanBundleDate,

        [Description("Cannot get local settings date, probably application is not installed.")]
        CannotGetLocalSettingsDate
    }
}
