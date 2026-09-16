using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Models;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.Controllers
{
    /// <summary>The "no ads" switch. Turning it off loads the interstitial that was not being loaded.</summary>
    internal class SetAdsRemovedCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }
        [InjectSignal] private AdsInternalSignals _signals { get; set; }

        [SignalParam] private bool _removed { get; set; }

        public override void Execute()
        {
            _model.SetAdsRemoved(_removed);
            FlowLogger.Log($"Ads removed - {_removed}");

            if (!_removed)
                _signals.Load.Dispatch(AdFormat.Interstitial);
        }
    }
}
