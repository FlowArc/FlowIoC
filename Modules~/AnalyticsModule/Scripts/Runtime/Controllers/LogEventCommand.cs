using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Enums;
using Modules.AnalyticsModule.Models;

namespace Modules.AnalyticsModule.Controllers
{
    /// <summary>
    /// One event to every slot: straight through when Ready, queued while Plugged or
    /// Initializing, skipped when Failed (the failure was reported at the flush). The event is
    /// logged here once, whatever the slots, so a flow reads in the console with no SDK at all;
    /// a scene with no provider says so, as a plain log, because a test scene without an SDK is
    /// a legitimate scene. One event that a provider rejects does not close the provider.
    /// </summary>
    internal class LogEventCommand : Command
    {
        [Inject] private IAnalyticsModel _model { get; set; }

        [SignalParam] private AnalyticsEventVO _event { get; set; }

        public override void Execute()
        {
            if (_event == null || string.IsNullOrEmpty(_event.Name))
            {
                FlowLogger.LogError("Log - an event with no name was handed in; nothing sent.");
                return;
            }

            FlowLogger.Log($"Log - {_event}");

            if (_model.Slots.Count == 0)
            {
                FlowLogger.Log("Log - no provider plugged; the event went nowhere.");
                return;
            }

            foreach (ProviderSlotVO slot in _model.Slots)
            {
                switch (slot.State)
                {
                    case AnalyticsProviderState.Ready:
                        try
                        {
                            slot.Provider.Log(_event);
                        }
                        catch (Exception exception)
                        {
                            FlowLogger.LogError($"Log - '{slot.Provider.Name}' threw on '{_event.Name}': {exception.Message}");
                        }

                        break;

                    case AnalyticsProviderState.Plugged:
                    case AnalyticsProviderState.Initializing:
                        if (slot.Enqueue(_event) && slot.Dropped == 1)
                            FlowLogger.Log($"Log - '{slot.Provider.Name}' has {ProviderSlotVO.PENDING_CAP} events waiting; the oldest are being dropped until it is ready.");

                        break;

                    case AnalyticsProviderState.Failed:
                        break;
                }
            }
        }
    }
}
