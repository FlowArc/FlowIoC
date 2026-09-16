#if UNITY_EDITOR

using System.Text;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.AdsTestModule.Models;
using Modules.AdsModule.AdsTestModule.Services;
using Modules.AdsModule.AdsTestModule.Signals;
using Modules.AdsModule.Services;
using Modules.AdsModule.Shared.Enums;

namespace Modules.AdsModule.AdsTestModule.Controllers
{
    /// <summary>What the service knows and what the fake holds, as the lines the label shows.</summary>
    public class ReportAdsStateCommand : Command
    {
        [Inject] private IAdsService _ads { get; set; }
        [Inject] private FakeAdsProvider _provider { get; set; }
        [Inject] private AdsTestModel _testModel { get; set; }
        [InjectSignal] private AdsTestInternalSignals _signals { get; set; }

        public override void Execute()
        {
            var text = new StringBuilder();
            text.Append("Provider: ").Append(_ads.ProviderName.Length == 0 ? "none" : _ads.ProviderName)
                .Append(' ').Append(_ads.ProviderState).Append('\n');
            text.Append("Fake holds the answer: ").Append(_provider.IsHoldingTheAnswer ? "yes" : "no").Append('\n');
            text.Append("Ready: rewarded ").Append(_ads.IsReady(AdFormat.Rewarded) ? "yes" : "no")
                .Append(", interstitial ").Append(_ads.IsReady(AdFormat.Interstitial) ? "yes" : "no").Append('\n');
            text.Append("Ads removed: ").Append(_ads.AdsRemoved ? "yes" : "no")
                .Append("   Loads fail: ").Append(_provider.LoadsFail ? "yes" : "no")
                .Append("   Silent show: ").Append(_provider.SilentShow ? "yes" : "no").Append('\n');
            text.Append(_provider.OnScreen.HasValue
                    ? $"AD ON SCREEN: {_provider.OnScreen.Value}/{_provider.OnScreenPlacement}"
                    : "No ad on screen")
                .Append('\n');
            text.Append("Last result: ").Append(_testModel.LastResult).Append('\n');
            text.Append("Last outgoing: ").Append(_testModel.LastOutgoing).Append('\n');

            _signals.StateChanged.Dispatch(text.ToString());
        }
    }
}

#endif
