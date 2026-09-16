using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Enums;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;
using Modules.AdsModule.Shared.Enums;
using Modules.AdsModule.Signals;
using UnityEngine;

namespace Modules.AdsModule.Controllers
{
    /// <summary>
    /// The one place a show is decided. Five refusals in order - no provider ready, another ad on
    /// screen, ads removed, too soon, nothing loaded - each answered in this call; past them the
    /// show begins, the provider is told, and a timeout watches for a provider that never answers.
    /// The watch works from locals: this step is not retained, so its instance is back in the
    /// pool while the wait runs.
    /// </summary>
    internal class ShowAdCommand : Command
    {
        [Inject] private IAdsModel _model { get; set; }
        [Inject] private ICoroutineProvider _coroutines { get; set; }
        [InjectSignal] private AdsInternalSignals _signals { get; set; }
        [InjectSignal] private AdsSignals _ads { get; set; }

        [SignalParam] private AdFormat _format { get; set; }
        [SignalParam] private string _placement { get; set; }
        [SignalParam] private Action<AdResultVO> _done { get; set; }

        public override void Execute()
        {
            string placement = _placement ?? string.Empty;
            FlowLogger.Log($"Show - {_format}/{placement}");

            IAdsProvider provider = _model.Provider;

            if (provider == null)
            {
                Refuse(AdOutcome.NotReady, "no provider plugged");
                return;
            }

            if (_model.ProviderState != AdsProviderState.Ready)
            {
                Refuse(AdOutcome.NotReady, $"provider not ready: {_model.ProviderState}");
                return;
            }

            if (_model.Current != null)
            {
                Refuse(AdOutcome.NotReady, $"another ad is on screen: {_model.Current}");
                return;
            }

            if (_format == AdFormat.Interstitial && _model.AdsRemoved)
            {
                Refuse(AdOutcome.Skipped, "ads removed");
                return;
            }

            if (_format == AdFormat.Interstitial && IsTooSoon(out float left))
            {
                Refuse(AdOutcome.Skipped, $"{left:0} s until the next interstitial");
                return;
            }

            AdSlotVO slot = _model.GetSlot(_format);

            if (slot.State != AdLoadState.Ready || !IsReadyAt(provider))
            {
                // Ready in the model but not in the SDK: the ad expired under us, so it is loaded again.
                bool reload = slot.State == AdLoadState.Ready || slot.State == AdLoadState.Idle;

                if (slot.State == AdLoadState.Ready)
                    slot.State = AdLoadState.Idle;

                Refuse(AdOutcome.NotReady, $"no {_format} loaded");

                if (reload)
                    _signals.Load.Dispatch(_format);

                return;
            }

            ShowVO show = _model.BeginShow(_format, placement, _done);
            slot.State = AdLoadState.Idle;
            _ads.Outgoing.ReadyChanged.Dispatch(_format, false);

            try
            {
                provider.Show(_format, placement);
            }
            catch (Exception exception)
            {
                _signals.DisplayFailed.Dispatch(_format, exception.Message);
                return;
            }

            float timeout = _model.Settings.ShowTimeoutSeconds;

            if (timeout <= 0f)
                return;

            IAdsModel model = _model;
            AdsInternalSignals signals = _signals;
            AdFormat format = _format;
            string name = provider.Name;
            int token = show.Token;

            _coroutines.WaitForSecondsRealTime(timeout, () =>
            {
                ShowVO current = model.Current;

                if (current == null || current.Token != token || current.Displayed)
                    return;

                signals.DisplayFailed.Dispatch(format, $"no answer from '{name}' in {timeout:0} s");
            });
        }

        private bool IsTooSoon(out float left)
        {
            float interval = _model.Settings.InterstitialMinIntervalSeconds;
            left = 0f;

            if (interval <= 0f)
                return false;

            float elapsed = Time.realtimeSinceStartup - _model.LastInterstitialTime;

            if (elapsed >= interval)
                return false;

            left = interval - elapsed;
            return true;
        }

        private bool IsReadyAt(IAdsProvider provider)
        {
            try
            {
                return provider.IsReady(_format);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Show - '{provider.Name}' threw on IsReady: {exception.Message}");
                return false;
            }
        }

        private void Refuse(AdOutcome outcome, string reason)
        {
            var result = new AdResultVO(_format, _placement, outcome, reason);
            FlowLogger.Log($"Show - {result}");

            try
            {
                _done?.Invoke(result);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"Show - the callback for '{result.Placement}' threw: {exception}");
            }
        }
    }
}
