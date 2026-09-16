using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;
using UnityEngine;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// The ad left the screen: the outcome is decided from what was heard - Rewarded, Completed
    /// for an interstitial, Dismissed for a rewarded ad without its reward - the game hears
    /// Closed, the asker is answered, and the next ad of the format is loaded. A callback that
    /// throws is reported once and does not stop the reload.
    /// </summary>
    internal class FinishShowCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }
        [InjectSignal] private AdsInternalSignals _signals { get; set; }
        [InjectSignal] private AdsSignals _ads { get; set; }

        [SignalParam] private AdFormat _format { get; set; }

        public override void Execute()
        {
            ShowVO current = _model.Current;

            if (current == null || current.Format != _format)
            {
                FlowLogger.Log($"Closed - {_format} with no show in progress; loading the next.");
                _signals.Load.Dispatch(_format);
                return;
            }

            AdOutcome outcome = current.Rewarded
                ? AdOutcome.Rewarded
                : _format == AdFormat.Interstitial ? AdOutcome.Completed : AdOutcome.Dismissed;

            var result = new AdResultVO(_format, current.Placement, outcome, null, current.Reward);
            FlowLogger.Log($"Closed - {result}");

            if (_format == AdFormat.Interstitial)
                _model.MarkInterstitialShown(Time.realtimeSinceStartup);

            _model.EndShow();
            _ads.Outgoing.Closed.Dispatch(_format, current.Placement);

            try
            {
                current.Done?.Invoke(result);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Closed - the callback for '{current.Placement}' threw: {exception}");
            }

            _signals.Load.Dispatch(_format);
        }
    }
}
