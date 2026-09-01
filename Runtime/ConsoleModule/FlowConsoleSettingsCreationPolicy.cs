namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// Decides whether a missing <see cref="CD_FlowConsole"/> should be written to disk.
    /// A settings asset that fails to load is not the same thing as a settings asset that is
    /// absent: Unity returns null for both, but the first happens whenever the script behind
    /// the asset cannot be resolved - during a compile failure, or after the package's asset
    /// paths change. Creating a fresh asset in that state overwrites the user's log types
    /// permanently, so the file on disk gets the benefit of the doubt.
    /// </summary>
    internal class FlowConsoleSettingsCreationPolicy
    {
        internal bool ShouldCreate(bool assetLoaded, bool fileExistsOnDisk)
        {
            return !assetLoaded && !fileExistsOnDisk;
        }
    }
}
