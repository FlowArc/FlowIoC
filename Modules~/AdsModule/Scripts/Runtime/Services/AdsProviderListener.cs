using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Shared.Data.ValueObjects;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.Services
{
    /// <summary>
    /// The one listener: each report becomes the matching internal signal, so every SDK callback
    /// is a step in the Flow Console. A click is logged and nothing more - it is not a flow.
    /// </summary>
    internal class AdsProviderListener : IAdsProviderListener
    {
        [InjectSignal] private AdsInternalSignals _signals { get; set; }

        public void OnInitialized(bool ready) => _signals.ProviderInitialized.Dispatch(ready);

        public void OnLoaded(AdFormat format) => _signals.Loaded.Dispatch(format);

        public void OnLoadFailed(AdFormat format, string error) => _signals.LoadFailed.Dispatch(format, error ?? string.Empty);

        public void OnDisplayed(AdFormat format) => _signals.Displayed.Dispatch(format);

        public void OnDisplayFailed(AdFormat format, string error) => _signals.DisplayFailed.Dispatch(format, error ?? string.Empty);

        public void OnClicked(AdFormat format) => FlowLogger.Log($"Clicked - {format}");

        public void OnRewardEarned(AdRewardVO reward) => _signals.RewardEarned.Dispatch(reward);

        public void OnClosed(AdFormat format) => _signals.Closed.Dispatch(format);

        public void OnRevenuePaid(AdRevenueVO revenue) => _signals.RevenuePaid.Dispatch(revenue);
    }
}
