#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace FlowIoC.Editor.Addressables
{
    /// <summary>
    /// Puts one entry into the project's Addressables groups. Everything decided about the entry
    /// was decided by ScreenAddressableEntries; this only talks to Unity.
    ///
    /// GetSettings(true) creates Assets/AddressableAssetsData when the project has none, which is
    /// the case in a project that has never opened the Addressables window. com.unity.addressables
    /// is a dependency of the package, so it is always there to be asked.
    /// </summary>
    internal class ScreenAddressables
    {
        internal void Register(ScreenAddressableEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.AssetPath))
                return;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            if (settings == null)
            {
                Debug.LogError("<color=cyan>[FlowIoC]</color> AddressableAssetSettings could not be created, "
                               + $"so '{entry.AssetPath}' was not made addressable.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(entry.AssetPath);

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"<color=cyan>[FlowIoC]</color> No asset found at '{entry.AssetPath}', "
                               + "so it was not made addressable.");
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(entry.GroupName)
                                          ?? settings.CreateGroup(entry.GroupName, false, false, false,
                                              settings.DefaultGroup.Schemas);

            AddressableAssetEntry created = settings.CreateOrMoveEntry(guid, group, readOnly: false);

            if (created == null)
            {
                Debug.LogError($"<color=cyan>[FlowIoC]</color> '{entry.AssetPath}' could not be added to "
                               + $"'{entry.GroupName}'.");
                return;
            }

            created.SetAddress(entry.Address);

            if (!string.IsNullOrEmpty(entry.Label))
                created.SetLabel(entry.Label, true, true);

            EditorUtility.SetDirty(settings);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, created, true);
        }
    }
}

#endif
