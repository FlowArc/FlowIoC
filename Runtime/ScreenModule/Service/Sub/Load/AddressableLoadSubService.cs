using System;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.ViewsMediators.Utils;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;

namespace FlowIoC.ScreenModule.Service.Sub.Load
{
    /// <summary>
    /// The addressable half of loading a screen. It keeps no cache of its own: the prefab comes
    /// from IAssetService under the owner "Screen/" + manager id, so a prefab a pool also holds is
    /// loaded once and released when the last of them lets go, and a screen registered at two
    /// managers holds it twice. The service is resolved at the first load rather than injected:
    /// AssetServiceRoot is optional in a scene whose screens are all Resource screens, and
    /// initialises after ScreenServiceRoot when it is there.
    /// </summary>
    internal class AddressableLoadSubService
    {
        [Inject] private InjectionBinderCrossContext _crossContext { get; set; }

        private IAssetService _assets;

        public async Task<IScreenBody> LoadScreen(ScreenEntry entry)
        {
            string address = entry.Screen.Load.Key;
            string owner = OwnerOf(entry);

            if (!TryResolveAssets(entry))
                return null;

            try
            {
                FlowLogger.Log(SystemLogType.Screen, $"[AddressableLoadSubService.LoadScreen][address({address})][owner({owner})]");

                GameObject prefab = await _assets.LoadAssetAsync<GameObject>(address, owner);

                // Two loads of one entry are one screen: the second await lands after the first
                // instantiated, and a second instance would be a leak nothing holds.
                if (entry.Loaded != null)
                    return entry.Loaded;

                if (prefab == null)
                {
                    FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] Failed to load addressable asset for {address}");
                    return null;
                }

                GameObject screenInstance = UnityEngine.Object.Instantiate(prefab);
                screenInstance.SetActive(false);

                IScreenBody screenBody = screenInstance.GetComponent<IScreenBody>();

                if (screenBody == null)
                {
                    FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] IScreenBody component not found on prefab for {address}");
                    UnityEngine.Object.Destroy(screenInstance);
                    _assets.Release(address, owner);
                    return null;
                }

                entry.Loaded = screenBody;
                return screenBody;
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[AddressableLoadService] Error loading {address}: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        public void UnloadScreen(ScreenEntry entry, IScreenBody screenBody)
        {
            entry.Loaded = null;
            _assets?.Release(entry.Screen.Load.Key, OwnerOf(entry));

            // Not == null: an IScreenBody is an interface, so that compares the managed reference
            // and says a screen Unity destroyed on play mode exit is still there.
            if (!screenBody.IsAlive()) return;

            try
            {
                UnityEngine.Object.Destroy(screenBody.gameObject);
            }
            catch (Exception e)
            {
                FlowLogger.LogError(SystemLogType.Screen, $"[ScreenService] Error unloading addressable screen: {e.Message}\n{e.StackTrace}");
            }
        }

        private static string OwnerOf(ScreenEntry entry) => "Screen/" + entry.Screen.ManagerId;

        private bool TryResolveAssets(ScreenEntry entry)
        {
            if (_assets != null)
                return true;

            if (!_crossContext.HasBinding<IAssetService>())
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService.Load] '{entry.ViewType.Name}' is addressable and AssetServiceRoot is not in the scene. "
                    + "Put AssetServiceRoot in the scene; it is what loads addressables.");
                return false;
            }

            _assets = _crossContext.GetInstance<IAssetService>();
            return true;
        }
    }
}