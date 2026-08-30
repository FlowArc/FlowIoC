using FlowIoC.BaseModule.Signals;

namespace Modules.ScreenModule.Shared.Signals
{
    public class ScreenSignals : ISignalHolder
    {
        public ScreenSignalsIncoming Incoming = new();
        public ScreenSignalsOutgoing Outgoing = new();
    }

    public class ScreenSignalsIncoming
    {
    }

    public class ScreenSignalsOutgoing
    {
    }
}
