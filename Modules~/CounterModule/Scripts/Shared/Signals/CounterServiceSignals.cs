using FlowIoC.BaseModule.Signals;

namespace Modules.CounterModule.Shared.Signals
{
    public class CounterServiceSignals : ISignalHolder
    {
        public CounterServiceSignalsIncoming Incoming = new();
        public CounterServiceSignalsOutgoing Outgoing = new();
    }

    public class CounterServiceSignalsIncoming
    {
        /// <summary>
        /// Prepares the time source and starts the tick. The module's own context dispatches this
        /// on setup, so a game sends it again only to retry a source that failed the first time.
        /// </summary>
        public Signal Initialize = new();
    }

    public class CounterServiceSignalsOutgoing
    {
        /// <summary>
        /// Whether the time source made itself ready. False means nothing will tick: counters
        /// asked for meanwhile are held and start together once Initialize succeeds.
        /// </summary>
        public Signal<bool> Ready = new();
    }
}
