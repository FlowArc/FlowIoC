#if UNITY_EDITOR

using FlowIoC.BaseModule.Signals;

namespace Modules.LocalSaveModule.LocalSaveTestModule.Signals
{
    public class LocalSaveTestSignals : ISignalHolder
    {
        public LocalSaveTestSignalsIncoming Incoming = new();
        public LocalSaveTestSignalsOutgoing Outgoing = new();
    }

    public class LocalSaveTestSignalsIncoming
    {
        /// <summary>Shows whatever the probe holds, without changing it. Dispatched on launch.</summary>
        public Signal ReportProbe = new();

        /// <summary>Raises the counter by one and asks the save module to write it.</summary>
        public Signal IncrementProbe = new();
    }

    public class LocalSaveTestSignalsOutgoing
    {
        public Signal<int> CounterChanged = new();
    }
}

#endif