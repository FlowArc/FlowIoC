#if UNITY_EDITOR
using FlowIoC.Editor.Addressables;
using UnityEditor;
using UnityEngine;

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
        /// in a row and saves once at the end, and the generator has this one entry to save.
        /// </summary>
        private static void MakePrefabAddressable(string prefabPath, string prefabName)
        {
            ScreenAddressableEntry entry = new ScreenAddressableEntries().For(prefabName);
            entry.AssetPath = prefabPath.Replace(Application.dataPath, "Assets");

            new ScreenAddressables().Register(entry);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
