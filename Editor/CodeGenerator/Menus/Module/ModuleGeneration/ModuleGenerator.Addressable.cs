#if UNITY_EDITOR
using System.Linq;
using FlowIoC.Editor.Addressables;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        /// <summary>
        /// The registration a generated screen needs, done by the one class that knows how. The
        /// installer that brings the ready made setup modules calls the same pair, so a screen the
        /// generator wrote and a screen that arrived with the set are addressable in exactly the
        /// same way.
        ///
        /// This is the second of the two calls, so it is where the settings asset is written back
        /// out. ScreenAddressables leaves saving to its caller: the installer registers four
        /// entries in a row and saves once at the end.
        /// </summary>
        private static void MakeScreenConfigAddressable(string createdConfigPath, string prefabName)
        {
            ScreenAddressableEntry entry = new ScreenAddressableEntries()
                .For(prefabName)
                .First(candidate => candidate.GroupName == ScreenAddressableEntries.ConfigGroup);

            entry.AssetPath = createdConfigPath.Replace(Application.dataPath, "Assets");

            new ScreenAddressables().Register(entry);

            AssetDatabase.SaveAssets();
        }

        private static void MakePrefabAddressable(string prefabPath, string prefabName)
        {
            ScreenAddressableEntry entry = new ScreenAddressableEntries()
                .For(prefabName)
                .First(candidate => candidate.GroupName != ScreenAddressableEntries.ConfigGroup);

            entry.AssetPath = prefabPath.Replace(Application.dataPath, "Assets");

            new ScreenAddressables().Register(entry);
        }
    }
}
#endif