#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Signals
{
    /// <summary>
    /// The sample screen's public holder, which every screen module has. Nothing crosses it: the
    /// test module opens the screen through the screen service, and the screen talks to ResourceFly
    /// through the service's interface and steps, not through a Connector.
    /// </summary>
    public class ResourceFlySampleScreenSignals : ISignalHolder
    {
        public ResourceFlySampleScreenSignalsIncoming Incoming = new();
        public ResourceFlySampleScreenSignalsOutgoing Outgoing = new();
    }

    public class ResourceFlySampleScreenSignalsIncoming
    {
    }

    public class ResourceFlySampleScreenSignalsOutgoing
    {
    }
}
#endif