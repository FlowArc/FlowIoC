#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    internal partial class ModuleGenerator
    {
        private static void MakeScreenConfigAddressable(string createdConfigPath, string prefabName)
        {
            createdConfigPath = createdConfigPath.Replace(Application.dataPath, "Assets");

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings could not be found. Please set up Addressables properly.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(createdConfigPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"No valid GUID found for the screen config: {createdConfigPath}");
                return;
            }

            AddressableAssetGroup group = settings.FindGroup("Local_Screen-Configs");
            if (group == null)
            {
                group = settings.CreateGroup(
                    "Local_Screen-Configs",
                    false,
                    false,
                    false,
                    settings.DefaultGroup.Schemas
                );
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false);
            if (entry == null)
            {
                Debug.LogError($"Failed to create or move the addressable entry: {createdConfigPath}");
                return;
            }

            entry.SetAddress(prefabName + "Config");
            entry.SetLabel("ScreenConfig", true, true);

            EditorUtility.SetDirty(settings);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            AssetDatabase.SaveAssets();
            Debug.Log($"ScreenConfig '{prefabName}Config' is now Addressable with address: {entry.address}");
        }

        private static void MakePrefabAddressable(string prefabPath, string prefabName)
        {
            prefabPath = "Assets/" + prefabPath.Replace(Application.dataPath, "");

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings could not be found. Please set up Addressables properly.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"No valid GUID found for the prefab: {prefabPath}");
                return;
            }

            string newPrefabName = prefabName.Replace("Screen", "");

            AddressableAssetGroup group = settings.FindGroup($"Local_Screen-{newPrefabName}");
            if (group == null)
            {
                group = settings.CreateGroup(
                    $"Local_Screen-{newPrefabName}",
                    false,
                    false,
                    false,
                    settings.DefaultGroup.Schemas
                );
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false);
            if (entry == null)
            {
                Debug.LogError($"Failed to create or move the addressable entry: {prefabPath}");
                return;
            }

            entry.SetAddress(prefabName);
            entry.SetLabel("ScreenPrefab", true, true);

            EditorUtility.SetDirty(settings);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            AssetDatabase.SaveAssets();
            Debug.Log($"Prefab '{prefabName}' is now Addressable with address: {entry.address}");
        }
    }
}
#endif