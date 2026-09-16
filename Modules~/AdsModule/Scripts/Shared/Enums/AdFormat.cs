namespace Modules.AdsModule.Shared.Enums
{
    /// <summary>
    /// The shapes an ad takes. Shared, because the Outgoing signals carry it. Banner takes 2 the
    /// day it comes; a number is never reused.
    /// </summary>
    public enum AdFormat
    {
        Rewarded = 0,
        Interstitial = 1
    }
}
