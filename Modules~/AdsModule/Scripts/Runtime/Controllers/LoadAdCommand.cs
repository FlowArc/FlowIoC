using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// One load per format at a time. Nothing while the provider is not ready, while a load or a
    /// retry is already under way, or for an interstitial once ads are removed - no request
    /// wasted on an ad that will not show. A Load that throws is a load that failed.
    /// </summary>
    internal class LoadAdCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }
        [InjectSignal] private AdsInternalSignals _signals { get; set; }

        [SignalParam] private AdFormat _format { get; set; }

        public override void Execute()
        {
            IAdsProvider provider = _model.Provider;

            if (provider == null || _model.ProviderState != AdsProviderState.Ready)
            {
                FlowLogger.Log($"Load - {_format} skipped: provider {(provider == null ? "not plugged" : _model.ProviderState.ToString())}.");
                return;
            }

            AdSlotVO slot = _model.GetSlot(_format);

            if (slot.State != AdLoadState.Idle)
                return;

            if (_format == AdFormat.Interstitial && _model.AdsRemoved)
            {
                FlowLogger.Log("Load - Interstitial skipped: ads removed.");
                return;
            }

            slot.State = AdLoadState.Loading;
            FlowLogger.Log($"Load - {_format}");

            try
            {
                provider.Load(_format);
            }
            catch (Exception exception)
            {
                _signals.LoadFailed.Dispatch(_format, exception.Message);
            }
        }
    }
}
