using System;
using UnityEngine;

namespace Modules.AdsModule.AppLovinMaxAdsModule.Data.ValueObjects
{
    /// <summary>One MAX ad unit, one id per store - what the AppLovin dashboard hands out per format.</summary>
    [Serializable]
    public class AdUnitIdCVO
    {
        [Tooltip("The MAX ad unit id for the Android app.")]
        public string Android = string.Empty;

        [Tooltip("The MAX ad unit id for the iOS app.")]
        public string Ios = string.Empty;
    }
}
