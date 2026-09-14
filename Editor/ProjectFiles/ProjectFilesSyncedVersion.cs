#if UNITY_EDITOR
using UnityEditor;

namespace FlowIoC.Editor.ProjectFiles
{
    /// <summary>
    /// The FlowIoC version this machine last regenerated the project files for, in this project.
    ///
    /// Per person and per project, like the What's New record: the solution and project files are
    /// this machine's, not the repository's, so the version they were last written for is nothing
    /// a committed marker could carry. EditorPrefs is shared by every project a Unity install
    /// opens, so the key carries the project it is about.
    /// </summary>
    internal class ProjectFilesSyncedVersion
    {
        private const string KEY_PREFIX = "FlowIoC.ProjectFiles.SyncedVersion.";

        private readonly string _key;

        internal ProjectFilesSyncedVersion(string projectRoot)
        {
            _key = KEY_PREFIX + (projectRoot ?? string.Empty).Replace('\\', '/').ToLowerInvariant();
        }

        internal string Read() => EditorPrefs.GetString(_key, string.Empty);

        internal void Write(string version) => EditorPrefs.SetString(_key, version ?? string.Empty);

        /// <summary>For a test that used a root of its own, so the record does not outlive it.</summary>
        internal void Forget() => EditorPrefs.DeleteKey(_key);
    }
}
#endif
