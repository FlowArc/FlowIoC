#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Android;
using UnityEditor.AddressableAssets.Settings;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>
    /// Whether Init Play Asset Delivery has run: the Android package's own test, reproduced
    /// because it keeps it internal - its build script and its initialisation settings are both
    /// listed in the Addressables settings.
    /// </summary>
    internal class PlayAssetDeliveryState
    {
        public bool IsInitialized(AddressableAssetSettings settings)
        {
            if (settings == null) return false;

            return settings.InitializationObjects.FindIndex(i => i is PlayAssetDeliveryInitializationSettings) != -1
                   && settings.DataBuilders.FindIndex(b => b is BuildScriptPlayAssetDelivery) != -1;
        }
    }
}
#endif
