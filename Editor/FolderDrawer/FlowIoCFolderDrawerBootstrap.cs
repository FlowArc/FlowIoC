#if UNITY_EDITOR

using UnityEditor;

namespace FlowIoC.FolderDrawer
{
    /// <summary>
    /// The only static surface of the folder drawer. Unity's load hook has to be static,
    /// so this type holds the single drawer instance the editor callbacks need and does
    /// nothing else; the behaviour lives on <see cref="FlowIoCFolderDrawer"/>.
    /// </summary>
    internal static class FlowIoCFolderDrawerBootstrap
    {
        private const string CONFIG_CHECKED_KEY = "FlowIoCFolderDrawer_ConfigChecked";

        private static FlowIoCFolderDrawer _drawer;

        public static FlowIoCFolderDrawer Drawer => _drawer ??= new FlowIoCFolderDrawer();

        [InitializeOnLoadMethod]
        private static void OnProjectLoad()
        {
            // The AssetDatabase is not safe to write to while the domain is still reloading,
            // so the config is created on a later editor tick. EditorApplication.update is used
            // rather than delayCall, because delayCall is only pumped by the editor GUI loop and
            // never fires while the Editor sits unfocused or minimized.
            EditorApplication.update -= Bootstrap;
            EditorApplication.update += Bootstrap;
        }

        private static void Bootstrap()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;

            EditorApplication.update -= Bootstrap;

            // Creating the asset is a once per session concern; repainting is not.
            if (!SessionState.GetBool(CONFIG_CHECKED_KEY, false))
            {
                SessionState.SetBool(CONFIG_CHECKED_KEY, true);
                Drawer.EnsureConfig();
            }

            Drawer.Apply();
        }
    }
}
#endif
