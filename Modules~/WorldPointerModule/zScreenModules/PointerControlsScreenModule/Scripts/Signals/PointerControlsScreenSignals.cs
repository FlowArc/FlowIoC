#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.WorldPointerModule.PointerControlsScreenModule.Signals
{
    /// <summary>
    /// The sample's control panel: what its buttons say leaves on Outgoing, and the count it shows
    /// arrives on Incoming. The WorldPointer test module listens and answers - it may reference
    /// anything - so the panel knows nothing about pointers.
    /// </summary>
    public class PointerControlsScreenSignals : ISignalHolder
    {
        public PointerControlsScreenSignalsIncoming Incoming = new();
        public PointerControlsScreenSignalsOutgoing Outgoing = new();
    }

    public class PointerControlsScreenSignalsIncoming
    {
        /// <summary>How many targets the service holds.</summary>
        public Signal<int> ShowCount = new();
    }

    public class PointerControlsScreenSignalsOutgoing
    {
        public Signal RegisterPressed = new();
        public Signal ChangeContentPressed = new();
        public Signal<bool> HiddenToggled = new();
        public Signal ToggleScreenPressed = new();
        public Signal UnregisterAllPressed = new();
    }
}
#endif