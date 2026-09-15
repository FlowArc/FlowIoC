using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Util;
#if UNITY_IOS && !UNITY_EDITOR
using System.IO;
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Constants;
using Modules.AssetDeliveryModule.Data.ValueObjects;
using Modules.AssetDeliveryModule.Gateway;
using UnityEngine;
#endif

namespace Modules.AssetDeliveryModule.Initialization
{
    /// <summary>
    /// Runs inside Addressables' own initialisation, listed through ED_AssetPackInitialization in
    /// the settings' Initialization Objects. On iOS it reads FlowAssetPacks.json beside the
    /// catalog and points every bundle the manifest lists at the file Background Assets holds it
    /// in. Everywhere else it does nothing: Android has Unity's own transform, the Editor has the
    /// bundles where the build wrote them.
    /// </summary>
    public class AssetPackInitialization : IInitializableObject
    {
        public bool Initialize(string id, string data)
        {
#if UNITY_IOS && !UNITY_EDITOR
            string path = Path.Combine(UnityEngine.AddressableAssets.Addressables.RuntimePath, AssetDeliveryConstants.MANIFEST_FILE);

            if (!File.Exists(path))
            {
                FlowLogger.Log("AssetPackInitialization - no FlowAssetPacks.json beside the catalog, so no bundle is read from an asset pack.");
                return true;
            }

            AssetPackManifestVO manifest = JsonUtility.FromJson<AssetPackManifestVO>(File.ReadAllText(path));
            var map = new AssetPackPathMap(manifest.Packs, FlowAssetPacksBridge.PathForFile);
            UnityEngine.AddressableAssets.Addressables.ResourceManager.InternalIdTransformFunc = location => map.Transform(location.InternalId);
            FlowLogger.Log($"AssetPackInitialization - {manifest.Packs.Count} pack(s) read from Background Assets.");
#endif
            return true;
        }

        public AsyncOperationHandle<bool> InitializeAsync(ResourceManager rm, string id, string data) =>
            rm.CreateCompletedOperation(Initialize(id, data), string.Empty);
    }
}
