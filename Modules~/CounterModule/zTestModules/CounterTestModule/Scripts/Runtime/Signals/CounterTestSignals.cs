#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.CounterModule.CounterTestModule.Signals
{
    public class CounterTestSignals : ISignalHolder
    {
        public CounterTestSignalsIncoming Incoming = new();
        public CounterTestSignalsOutgoing Outgoing = new();
    }

    public class CounterTestSignalsIncoming
    {
        public Signal StartTestCounter = new();
        public Signal StopTestCounter = new();
    }

    public class CounterTestSignalsOutgoing
    {
        /// <summary>Seconds left, once a second.</summary>
        public Signal<float> Ticked = new();

        /// <summary>Seconds since the counter began, once a second.</summary>
        public Signal<float> Elapsed = new();

        public Signal Completed = new();
        public Signal Stopped = new();

        /// <summary>Whether the counter service was running when the counter was asked for.</summary>
        public Signal<bool> ServiceActive = new();
    }
}
#endif
