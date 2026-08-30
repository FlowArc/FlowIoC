using FlowIoC.BaseModule.Signals;

namespace Modules.GameplayModule.Shared.Signals
{
    public class GameplaySignals : ISignalHolder
    {
        public GameplaySignalsIncoming Incoming = new();
        public GameplaySignalsOutgoing Outgoing = new();
    }

    public class GameplaySignalsIncoming
    {
    }

    public class GameplaySignalsOutgoing
    {
    }
}
