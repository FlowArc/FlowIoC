using Modules.AdsModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.AdsModule.Data.UnityObjects
{
    /// <summary>
    /// The module's settings, filed on AdsServiceRoot's adapter under CD_Ads. The shipped asset
    /// carries the defaults; a game edits it in place.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_Ads", menuName = "FlowIoC/AdsModule/Data/CD_Ads")]
    public class CD_Ads : ScriptableObject
    {
        public AdsSettingsCVO Settings = new();
    }
}
