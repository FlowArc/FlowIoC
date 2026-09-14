#if UNITY_EDITOR
namespace FlowIoC.Editor.ProjectFiles
{
    /// <summary>
    /// When the project files are regenerated on the package's account: the first time a version
    /// of FlowIoC runs in a project on this machine, and never again for that version.
    ///
    /// The Package Manager puts an updated package in a new folder under Library/PackageCache,
    /// and the IDE integration's incremental sync does not count that as a change - the solution
    /// and the modules' projects are rewritten, the package's own project keeps the old folder, and
    /// Rider reports every FlowIoC type unresolved while Unity compiles fine. A full regeneration
    /// is the fix Preferences offers by hand; this is that button, pressed once per version.
    /// </summary>
    internal class ProjectFilesSyncRule
    {
        internal bool ShouldRegenerate(string installedVersion, string syncedVersion)
        {
            // An unresolved package has no version to compare or to record, which is what an
            // embedded copy outside the Package Manager looks like.
            if (string.IsNullOrEmpty(installedVersion))
                return false;

            return installedVersion != syncedVersion;
        }
    }
}
#endif
