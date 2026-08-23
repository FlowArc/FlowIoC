#if UNITY_EDITOR

using UnityEditor;

namespace FlowIoC.Editor.Migration
{
    /// <summary>
    /// The only static surface of the migration. Unity's load hook has to be static, so this type
    /// does nothing but hand the work to <see cref="FlowIoCPathMigrator"/> on the first editor tick
    /// where the AssetDatabase is writable.
    ///
    /// EditorApplication.update is used rather than delayCall for the same reason
    /// FlowIoCFolderDrawerBootstrap uses it: delayCall is only pumped by the editor GUI loop and
    /// never fires while the Editor sits unfocused or minimized.
    /// </summary>
    internal static class FlowIoCPathMigrationBootstrap
    {
        [InitializeOnLoadMethod]
        private static void OnProjectLoad()
        {
            EditorApplication.update -= Run;
            EditorApplication.update += Run;
        }

        private static void Run()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;

            EditorApplication.update -= Run;

            new FlowIoCPathMigrator().MigrateIfNeeded();
        }
    }
}

#endif
