#if UNITY_EDITOR
using FlowIoC.Editor.AgentRules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ProjectFiles
{
    /// <summary>
    /// Holds the one instance Unity's load callback needs. Unity forces this entry point to be
    /// static; everything it does lives on <see cref="ProjectFilesStartupSync"/>.
    ///
    /// EditorApplication.update rather than delayCall, for the reason the migration bootstrap
    /// gives: delayCall is pumped by the GUI loop and does not fire while the Editor sits
    /// unfocused - and a developer who has just updated the package is usually looking at the
    /// IDE, which is exactly where the stale project files show.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectFilesStartupHook
    {
        static ProjectFilesStartupHook()
        {
            EditorApplication.update -= RunOnce;
            EditorApplication.update += RunOnce;
        }

        private static void RunOnce()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;

            EditorApplication.update -= RunOnce;
            new ProjectFilesStartupSync().Run();
        }
    }

    /// <summary>
    /// Regenerates the IDE's project files once per FlowIoC version, so that a package update
    /// does not leave the IDE on the package's old folder. See <see cref="ProjectFilesSyncRule"/>
    /// for why the IDE integration does not do this on its own.
    ///
    /// There is deliberately no session guard: the version is compared on every load and written
    /// once regenerated, so a domain reload finds nothing to do - and a package update inside one
    /// session, which is the one event this exists for, is not hidden behind a flag that survives
    /// it.
    /// </summary>
    internal class ProjectFilesStartupSync
    {
        private readonly string _installedVersion;
        private readonly ProjectFilesSyncedVersion _synced;
        private readonly IProjectFilesRegenerator _regenerator;
        private readonly bool _batchMode;

        internal ProjectFilesStartupSync() : this(
            new AgentRulesSource().Version,
            new ProjectFilesSyncedVersion(new ProjectRoot().Resolve()),
            new CodeEditorProjectFiles(),
            Application.isBatchMode)
        {
        }

        internal ProjectFilesStartupSync(string installedVersion, ProjectFilesSyncedVersion synced,
            IProjectFilesRegenerator regenerator, bool batchMode)
        {
            _installedVersion = installedVersion;
            _synced = synced;
            _regenerator = regenerator;
            _batchMode = batchMode;
        }

        internal void Run()
        {
            // A batch run has no IDE to keep in step, and nobody to read what it wrote.
            if (_batchMode)
                return;

            if (!new ProjectFilesSyncRule().ShouldRegenerate(_installedVersion, _synced.Read()))
                return;

            _regenerator.Regenerate();
            _synced.Write(_installedVersion);

            Debug.Log("[FlowIoC] Project files regenerated for " + _installedVersion + " through "
                      + _regenerator.EditorName + ", so the IDE follows the package.");
        }
    }
}
#endif
