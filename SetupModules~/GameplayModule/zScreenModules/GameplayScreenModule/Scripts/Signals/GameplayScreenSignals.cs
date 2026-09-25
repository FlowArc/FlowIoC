using FlowIoC.BaseModule.Signals;

namespace Modules.GameplayModule.GameplayScreenModule.Signals
{
    public class GameplayScreenSignals : ISignalHolder
    {
        public GameplayScreenSignalsIncoming Incoming = new();
        public GameplayScreenSignalsOutgoing Outgoing = new();
    }

    public class GameplayScreenSignalsIncoming
    {
        public Signal OpenGameplayScreen = new();
    }

    public class GameplayScreenSignalsOutgoing
    {
    }
}
