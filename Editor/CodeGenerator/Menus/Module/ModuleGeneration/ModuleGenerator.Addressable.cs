#if UNITY_EDITOR
using FlowIoC.Editor.Addressables;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        /// <summary>
        /// The registration a generated screen needs, done by the one class that knows how. The
        /// installer that brings the ready made setup modules calls the same entry, so a screen the
        /// generator wrote and a screen that arrived with the set are addressable in exactly the
        /// same way.
        ///
        /// ScreenAddressables leaves saving to its caller: the installer registers several entries
        /// in a row and saves once at the end, and the generator has this one entry to save. What
        /// is saved is what registering dirtied - the Addressables settings and the groups - and
        /// nothing else: SaveAssets would write every dirty asset in the Editor, somebody's
        /// half-edited data asset included, as a side effect of creating a module.
        /// </summary>
        private static void MakePrefabAddressable(string prefabPath, string prefabName)
        {
            // Converted by the one helper that reads any form of the path. A plain Replace of
            // Application.dataPath missed a parent written with backslashes or in another casing,
            // and the screen then logged "No asset found" instead of becoming addressable.
            ScreenAddressableEntry entry = new ScreenAddressableEntries().For(prefabName);
            entry.AssetPath = NamespaceUtility.GetUnityAssetPath(prefabPath);

            new ScreenAddressables().Register(entry);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;

            AssetDatabase.SaveAssetIfDirty(settings);

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group != null)
                    AssetDatabase.SaveAssetIfDirty(group);
            }
        }
    }
}
#endif