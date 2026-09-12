using System.Threading.Tasks;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.ConsoleModule;
using FlowIoC.PoolModule.Components;
using FlowIoC.PoolModule.Entities;
using UnityEngine;

namespace FlowIoC.PoolModule.Services.Sub.Load
{
    /// <summary>
    /// The addressable half of building a pool item. It keeps no cache of its own: the prefab
    /// comes from IAssetService under the owner "Pool/" + group key, so a prefab a screen also
    /// holds is loaded once, a group's release lets it go only when nobody else holds it, and a
    /// label preloaded silently earlier is found in memory here. The service is resolved at the
    /// first load rather than injected: AssetServiceRoot is optional in a scene whose pools hold
    /// only direct prefabs, and initialises after PoolServiceRoot when it is there.
    /// </summary>
    internal class AddressableLoadSubService
    {
        [Inject] private InjectionBinderCrossContext _crossContext { get; set; }

        private IAssetService _assets;

        public async Task<IPoolableItem> LoadItem(AssetReferenceSpawnableObject assetReference, string groupKey)
        {
            if (!IsValid(assetReference, "LoadItem") || !TryResolveAssets(assetReference))
                return null;

            string owner = OwnerOf(groupKey);
            FlowLogger.Log(SystemLogType.Pool, $"[AddressableLoadSubService.LoadItem][key({assetReference.AssetGUID})][owner({owner})]");

            GameObject prefab = await _assets.LoadAssetAsync<GameObject>(assetReference, owner);

            if (prefab == null)
            {
                FlowLogger.LogError(SystemLogType.Pool,
                    $"[AddressableLoadService] Failed to load addressable asset with key: {assetReference.AssetGUID}");
                return null;
            }

            GameObject instance = Object.Instantiate(prefab);
            IPoolableItem poolableItem = instance.GetComponent<IPoolableItem>();

            if (poolableItem != null)
                return poolableItem;

            FlowLogger.LogError(SystemLogType.Pool, "[AddressableLoadService] Loaded prefab does not have an IPoolableItem component.");
            Object.Destroy(instance);
            return null;
        }

        /// <summary>The claim without the instance, for a lazy item that wants its prefab in memory.</summary>
        public async Task PreloadItemAsync(AssetReferenceSpawnableObject assetReference, string groupKey)
        {
            if (!IsValid(assetReference, "PreloadItemAsync") || !TryResolveAssets(assetReference))
                return;

            GameObject prefab = await _assets.LoadAssetAsync<GameObject>(assetReference, OwnerOf(groupKey));

            if (prefab == null)
                FlowLogger.LogError(SystemLogType.Pool, $"[AddressableLoadService] Failed to preload asset with key: {assetReference.AssetGUID}");
            else
                FlowLogger.Log(SystemLogType.Pool, $"[AddressableLoadSubService.PreloadItemAsync][key({assetReference.AssetGUID})]");
        }

        public void UnloadItem(AssetReferenceSpawnableObject assetReference, string groupKey)
        {
            if (assetReference == null || !assetReference.RuntimeKeyIsValid()) return;

            _assets?.Release(assetReference, OwnerOf(groupKey));
        }

        private static string OwnerOf(string groupKey) => "Pool/" + groupKey;

        private static bool IsValid(AssetReferenceSpawnableObject assetReference, string call)
        {
            if (assetReference != null && assetReference.RuntimeKeyIsValid())
                return true;

            FlowLogger.LogError(SystemLogType.Pool, $"[AddressableLoadService] Invalid AssetReference in {call}.");
            return false;
        }

        private bool TryResolveAssets(AssetReferenceSpawnableObject assetReference)
        {
            if (_assets != null)
                return true;

            if (!_crossContext.HasBinding<IAssetService>())
            {
                FlowLogger.LogError(SystemLogType.Pool,
                    $"[PoolService] The pool item with key {assetReference.AssetGUID} is addressable and AssetServiceRoot is not in the scene. "
                    + "Put AssetServiceRoot in the scene; it is what loads addressables.");
                return false;
            }

            _assets = _crossContext.GetInstance<IAssetService>();
            return true;
        }
    }
}