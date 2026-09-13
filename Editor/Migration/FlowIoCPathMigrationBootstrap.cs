#if UNITY_EDITOR

using FlowIoC.Editor.Console;
using UnityEditor;

namespace FlowIoC.Editor.Migration
{
    /// <summary>
    /// The only static surface of the migration. Unity's load hook has to be static, so this type
    /// does nothing but hand the work to <see cref="FlowIoCPathMigrator"/> on the first editor tick
    /// where the AssetDatabase is writable.
    ///
    /// EditorApplication.update is used rather than delayCall for the same reason
    /// FolderPainterBootstrap uses it: delayCall is only pumped by the editor GUI loop and
    /// never fires while the Editor sits unfocused or minimized.
    ///
    /// It runs the path migration through the FlowModule generator rather than on its own, because
    /// the two are one pass: the migration rewrites what the parts are referenced as and deletes
    /// the files an older FlowIoC wrote, and the generator writes the parts under the new name and
    /// sweeps the old ones - all before assemblies reload, or the compile between would see a
    /// reference with nothing declaring it. The screen config migrator needs the same footing.
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

            FlowModuleGenerator.Generate();
            new ScreenConfigMigrator().MigrateIfNeeded();
        }
    }
}

#endif