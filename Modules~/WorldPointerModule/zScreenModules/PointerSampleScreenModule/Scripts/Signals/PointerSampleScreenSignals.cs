#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.WorldPointerModule.PointerSampleScreenModule.Signals
{
    /// <summary>
    /// The sample screen's public holder, which every screen module has. Nothing crosses it: the
    /// screen is opened through the screen service by the test module, and the pointers reach it
    /// through WorldPointer, not through a Connector.
    /// </summary>
    public class PointerSampleScreenSignals : ISignalHolder
    {
        public PointerSampleScreenSignalsIncoming Incoming = new();
        public PointerSampleScreenSignalsOutgoing Outgoing = new();
    }

    public class PointerSampleScreenSignalsIncoming
    {
    }

    public class PointerSampleScreenSignalsOutgoing
    {
    }
}
#endif