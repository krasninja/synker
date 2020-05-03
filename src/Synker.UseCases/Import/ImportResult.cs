namespace Synker.UseCases.Import
{
    /// <summary>
    /// Import process result.
    /// </summary>
    public enum ImportResult
    {
        Success,

        SettingsDateNewerThanBundleDate,

        CannotGetLocalSettingsDate
    }
}
