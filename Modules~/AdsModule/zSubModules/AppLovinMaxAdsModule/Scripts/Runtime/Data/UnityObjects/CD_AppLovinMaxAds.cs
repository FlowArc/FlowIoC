using Modules.AdsModule.AppLovinMaxAdsModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.AdsModule.AppLovinMaxAdsModule.Data.UnityObjects
{
    /// <summary>
    /// The ad unit ids the MAX plug loads and shows. Filled by the game and filed once, in
    /// AdsServiceRoot's Shared Scriptables; the plug reads it through ISharedDataModel. The SDK
    /// key is not here - it lives in AppLovin's Integration Manager.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_AppLovinMaxAds", menuName = "FlowIoC/AppLovinMaxAdsModule/Data/CD_AppLovinMaxAds")]
    public class CD_AppLovinMaxAds : ScriptableObject
    {
        public AdUnitIdCVO Rewarded = new();
        public AdUnitIdCVO Interstitial = new();
    }
}
