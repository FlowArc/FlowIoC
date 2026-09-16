#if UNITY_EDITOR

using FlowIoC.BaseModule.Signals;

namespace Modules.AdsModule.AdsTestModule.Signals
{
    /// <summary>The test scene's buttons in, and the state text out.</summary>
    public class AdsTestInternalSignals : ISignalHolder
    {
        public Signal<bool> AnswerProvider = new();
        public Signal ShowRewarded = new();
        public Signal ShowInterstitial = new();
        public Signal RewardAndClose = new();
        public Signal Close = new();
        public Signal FailToDisplay = new();
        public Signal PayRevenue = new();
        public Signal<bool> SetLoadsFail = new();
        public Signal<bool> SetSilentShow = new();
        public Signal<bool> SetAdsRemoved = new();
        public Signal ConsentAll = new();
        public Signal Initialize = new();
        public Signal<string> OutgoingHeard = new();
        public Signal ReportState = new();
        public Signal<string> StateChanged = new();
    }
}

#endif
