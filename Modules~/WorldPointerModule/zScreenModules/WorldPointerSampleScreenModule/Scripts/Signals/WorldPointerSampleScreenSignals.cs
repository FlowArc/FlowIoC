#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.WorldPointerModule.WorldPointerSampleScreenModule.Signals
{
    /// <summary>
    /// The sample screen's public holder, which every screen module has. Nothing crosses it: the
    /// screen is opened through the screen service by the test module, and the pointers reach it
    /// through WorldPointer, not through a Connector.
    /// </summary>
    public class WorldPointerSampleScreenSignals : ISignalHolder
    {
        public WorldPointerSampleScreenSignalsIncoming Incoming = new();
        public WorldPointerSampleScreenSignalsOutgoing Outgoing = new();
    }

    public class WorldPointerSampleScreenSignalsIncoming
    {
    }

    public class WorldPointerSampleScreenSignalsOutgoing
    {
    }
}
#endif