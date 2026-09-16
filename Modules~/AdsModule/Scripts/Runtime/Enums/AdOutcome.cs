namespace Modules.AdsModule.Enums
{
    /// <summary>How a show ended, for whoever asked. Only Rewarded releases the shipped rewarded step.</summary>
    public enum AdOutcome
    {
        /// <summary>The rewarded ad was watched and the reward earned.</summary>
        Rewarded = 0,

        /// <summary>The interstitial was shown and closed.</summary>
        Completed = 1,

        /// <summary>The rewarded ad was closed before the reward.</summary>
        Dismissed = 2,

        /// <summary>Nothing to show: no provider, not initialized, not loaded, another ad on screen.</summary>
        NotReady = 3,

        /// <summary>The SDK tried to show and could not.</summary>
        Failed = 4,

        /// <summary>The module chose not to show: ads removed, too soon after the last interstitial.</summary>
        Skipped = 5
    }
}
