#if UNITY_EDITOR
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Initialization;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>
    /// Puts the module's ED_AssetPackInitialization into the Addressables settings'
    /// Initialization Objects, once, so that settings.json carries the iOS transform. The asset
    /// ships with the module; only the listing is done here.
    /// </summary>
    internal class AssetDeliverySetup
    {
        private const string ASSET_FILTER = "t:ED_AssetPackInitialization";

        public bool IsRegistered(AddressableAssetSettings settings)
        {
            foreach (ScriptableObject entry in settings.InitializationObjects)
                if (entry is ED_AssetPackInitialization)
                    return true;

            return false;
        }

        public void RegisterInitializationObject(AddressableAssetSettings settings)
        {
            if (settings == null || IsRegistered(settings)) return;

            string[] guids = AssetDatabase.FindAssets(ASSET_FILTER);

            if (guids.Length == 0)
            {
                FlowLogger.LogError("AssetDeliverySetup - the module's ED_AssetPackInitialization asset is missing, so Addressables cannot read bundles from iOS asset packs.");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<ED_AssetPackInitialization>(AssetDatabase.GUIDToAssetPath(guids[0]));
            settings.AddInitializationObject(asset);
            FlowLogger.Log("AssetDeliverySetup - ED_AssetPackInitialization listed under Addressables' Initialization Objects.");
        }
    }
}
#endif
