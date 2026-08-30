using FlowIoC.BaseModule.Signals;

namespace Modules.CountdownServiceModule.Shared.Signals
{
    public class CountdownServiceSignals : ISignalHolder
    {
        public CountdownServiceSignalsIncoming Incoming = new();
        public CountdownServiceSignalsOutgoing Outgoing = new();
    }

    public class CountdownServiceSignalsIncoming
    {
        /// <summary>
        /// Prepares the time source and starts the tick. The module's own context dispatches this
        /// on setup, so a game sends it again only to retry a source that failed the first time.
        /// </summary>
        public Signal Initialize = new();
    }

    public class CountdownServiceSignalsOutgoing
    {
        /// <summary>
        /// Whether the time source made itself ready. False means nothing will tick: countdowns
        /// asked for meanwhile are held and start together once Initialize succeeds.
        /// </summary>
        public Signal<bool> Ready = new();
    }
}
