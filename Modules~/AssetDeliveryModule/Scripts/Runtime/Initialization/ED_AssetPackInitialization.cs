using UnityEngine;
using UnityEngine.ResourceManagement.Util;

namespace Modules.AssetDeliveryModule.Initialization
{
    /// <summary>
    /// The asset Addressables lists under Initialization Objects. Its one job is to put an
    /// AssetPackInitialization into settings.json at build time, the way the Android package's
    /// own does. The serialising call is Editor-only in Addressables, so the player build sees
    /// an empty provider; the player reads settings.json, never this asset.
    /// </summary>
    public class ED_AssetPackInitialization : ScriptableObject, IObjectInitializationDataProvider
    {
        public string Name => "FlowIoC asset packs";

        public ObjectInitializationData CreateObjectInitializationData()
        {
#if UNITY_EDITOR
            return ObjectInitializationData.CreateSerializedInitializationData(typeof(AssetPackInitialization), "FlowAssetPacks", null);
#else
            return default;
#endif
        }
    }
}