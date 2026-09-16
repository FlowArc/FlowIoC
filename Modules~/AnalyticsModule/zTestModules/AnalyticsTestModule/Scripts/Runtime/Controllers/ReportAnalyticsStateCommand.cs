#if UNITY_EDITOR

using System.Text;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AnalyticsModule.AnalyticsTestModule.Services;
using Modules.AnalyticsModule.AnalyticsTestModule.Signals;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.AnalyticsTestModule.Controllers
{
    /// <summary>What the service knows and what the recording provider received, as the lines the label shows.</summary>
    public class ReportAnalyticsStateCommand : Command
    {
        private const int LAST_EVENTS_SHOWN = 5;

        [Inject] private IAnalyticsService _analytics { get; set; }
        [Inject] private RecordingAnalyticsProvider _provider { get; set; }
        [InjectSignal] private AnalyticsTestInternalSignals _signals { get; set; }

        public override void Execute()
        {
            var text = new StringBuilder();
            text.Append("Slots: ").Append(_analytics.Slots.Count).Append('\n');

            foreach (ProviderSlotVO slot in _analytics.Slots)
            {
                text.Append("  ").Append(slot.Provider.Name)
                    .Append(": ").Append(slot.State)
                    .Append(", queued ").Append(slot.PendingCount)
                    .Append('\n');
            }

            text.Append("Recording holds the answer: ").Append(_provider.IsHoldingTheAnswer ? "yes" : "no").Append('\n');
            text.Append("Recording received: ").Append(_provider.Calls.Count).Append(" call(s), ")
                .Append(_provider.Events.Count).Append(" event(s)").Append('\n');

            int first = _provider.Events.Count > LAST_EVENTS_SHOWN ? _provider.Events.Count - LAST_EVENTS_SHOWN : 0;

            for (int i = first; i < _provider.Events.Count; i++)
                text.Append("  ").Append(_provider.Events[i]).Append('\n');

            _signals.StateChanged.Dispatch(text.ToString());
        }
    }
}

#endif
