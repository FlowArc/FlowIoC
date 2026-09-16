using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// The SDK could not show what it was asked, or never answered. The game hears Failed, the
    /// asker gets Failed with the reason, and the next ad is loaded. A plain line, not an error:
    /// the provider reported the SDK's failure once already.
    /// </summary>
    internal class FailShowCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }
        [InjectSignal] private AdsInternalSignals _signals { get; set; }
        [InjectSignal] private AdsSignals _ads { get; set; }

        [SignalParam] private AdFormat _format { get; set; }
        [SignalParam] private string _error { get; set; }

        public override void Execute()
        {
            string error = _error ?? string.Empty;
            ShowVO current = _model.Current;

            if (current == null || current.Format != _format)
            {
                FlowLogger.Log($"Show failed - {_format}: {error}; no show in progress, loading the next.");
                _signals.Load.Dispatch(_format);
                return;
            }

            var result = new AdResultVO(_format, current.Placement, AdOutcome.Failed, error);
            FlowLogger.Log($"Show failed - {result}");

            _model.EndShow();
            _ads.Outgoing.Failed.Dispatch(_format, current.Placement, error);

            try
            {
                current.Done?.Invoke(result);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Show failed - the callback for '{current.Placement}' threw: {exception}");
            }

            _signals.Load.Dispatch(_format);
        }
    }
}
