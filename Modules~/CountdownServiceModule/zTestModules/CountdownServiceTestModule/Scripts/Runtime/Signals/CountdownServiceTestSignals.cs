#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.CountdownServiceModule.CountdownServiceTestModule.Signals
{
    public class CountdownServiceTestSignals : ISignalHolder
    {
        public CountdownServiceTestSignalsIncoming Incoming = new();
        public CountdownServiceTestSignalsOutgoing Outgoing = new();
    }

    public class CountdownServiceTestSignalsIncoming
    {
        public Signal StartTestCountdown = new();
        public Signal StopTestCountdown = new();
    }

    public class CountdownServiceTestSignalsOutgoing
    {
        /// <summary>Seconds left, once a second.</summary>
        public Signal<float> Ticked = new();

        /// <summary>Seconds since the countdown began, once a second.</summary>
        public Signal<float> Elapsed = new();

        public Signal Completed = new();
        public Signal Stopped = new();

        /// <summary>Whether the countdown service was running when the countdown was asked for.</summary>
        public Signal<bool> ServiceActive = new();
    }
}
#endif
