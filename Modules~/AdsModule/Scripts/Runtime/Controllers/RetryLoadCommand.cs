using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
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
    /// A load that failed is tried again after 2^min(6, attempt) seconds - 2, 4, 8, 16, 32, 64,
    /// 64 ... - which is what MAX recommends; offline it knocks every 64 seconds for ever. A
    /// second failure while a retry is waiting is swallowed. The step holds the sequence for the
    /// wait, so this pooled instance is its own for the whole delay.
    /// </summary>
    internal class RetryLoadCommand : Command
    {
        private const int MAX_EXPONENT = 6;

        [Inject] private IAdsModel _model { get; set; }
        [Inject] private ICoroutineProvider _coroutines { get; set; }
        [InjectSignal] private AdsInternalSignals _signals { get; set; }

        [SignalParam] private AdFormat _format { get; set; }
        [SignalParam] private string _error { get; set; }

        public override void Execute()
        {
            AdSlotVO slot = _model.GetSlot(_format);

            if (slot.State == AdLoadState.WaitingRetry)
            {
                FlowLogger.Log($"Load failed - {_format}: {_error}; a retry is already waiting.");
                return;
            }

            if (slot.State != AdLoadState.Loading)
            {
                FlowLogger.Log($"Load failed - {_format}: {_error}; nothing was loading ({slot.State}), ignored.");
                return;
            }

            slot.Attempt++;
            slot.State = AdLoadState.WaitingRetry;
            float delay = Mathf.Pow(2f, Mathf.Min(MAX_EXPONENT, slot.Attempt));
            FlowLogger.Log($"Load failed - {_format}: {_error}; retry {slot.Attempt} in {delay:0} s.");

            Retain();
            AdFormat format = _format;

            _coroutines.WaitForSecondsRealTime(delay, () =>
            {
                if (slot.State == AdLoadState.WaitingRetry)
                    slot.State = AdLoadState.Idle;

                _signals.Load.Dispatch(format);
                Release();
            });
        }
    }
}
