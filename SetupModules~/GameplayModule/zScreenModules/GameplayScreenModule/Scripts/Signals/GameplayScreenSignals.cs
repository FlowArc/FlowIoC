using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.GameplayScreenModule.Signals
{
    public class GameplayScreenSignals : ISignalHolder
    {
        public GameplayScreenSignalsIncoming Incoming = new();
        public GameplayScreenSignalsOutgoing Outgoing = new();
    }

    public class GameplayScreenSignalsIncoming
    {
        public Signal<DifficultyType> OpenGameplayScreen = new();
    }

    public class GameplayScreenSignalsOutgoing
    {
    }
}
