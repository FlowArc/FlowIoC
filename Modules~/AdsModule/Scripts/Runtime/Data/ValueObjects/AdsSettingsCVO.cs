using System;
using UnityEngine;

namespace Modules.AdsModule.Data.ValueObjects
{
    /// <summary>The three knobs CD_Ads carries. The defaults are what a game gets without touching the asset.</summary>
    [Serializable]
    public class AdsSettingsCVO
    {
        [Tooltip("Initialize the plugged SDK when the Root launches. Off: the game binds IAdsService.Commands.Initialize after its consent step.")]
        public bool InitializeOnLaunch = true;

        [Tooltip("Seconds that must pass after an interstitial closes before the next one shows. 0: no pacing.")]
        public float InterstitialMinIntervalSeconds = 0f;

        [Tooltip("Seconds a show may go unanswered before it counts as failed. 0: no timeout.")]
        public float ShowTimeoutSeconds = 10f;
    }
}
