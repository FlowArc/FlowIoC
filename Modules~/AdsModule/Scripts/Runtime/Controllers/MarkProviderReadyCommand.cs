using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// The provider's answer. Ready: the muted switch if one is held, then one load per format.
    /// Not ready: Failed, and one error saying no ad will show - the SDK's own reason was the
    /// provider's line.
    /// </summary>
    internal class MarkProviderReadyCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }
        [InjectSignal] private AdsInternalSignals _signals { get; set; }

        [SignalParam] private bool _ready { get; set; }

        public override void Execute()
        {
            IAdsProvider provider = _model.Provider;

            if (provider == null)
            {
                FlowLogger.Log("Initialize - a provider answered after it was unplugged; ignored.");
                return;
            }

            if (!_ready)
            {
                _model.SetProviderState(AdsProviderState.Failed);
                FlowLogger.LogError($"Initialize - '{provider.Name}' reported it is not usable; no ad will show.");
                return;
            }

            _model.SetProviderState(AdsProviderState.Ready);
            FlowLogger.Log($"Initialize - {provider.Name} is ready.");

            if (_model.Muted.HasValue)
            {
                try
                {
                    provider.SetMuted(_model.Muted.Value);
                }
                catch (Exception exception)
                {
                    FlowLogger.LogError($"Initialize - '{provider.Name}' threw on SetMuted: {exception.Message}");
                }
            }

            _signals.Load.Dispatch(AdFormat.Rewarded);
            _signals.Load.Dispatch(AdFormat.Interstitial);
        }
    }
}
