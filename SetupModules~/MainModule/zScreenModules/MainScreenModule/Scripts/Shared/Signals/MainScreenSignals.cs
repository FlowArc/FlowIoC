using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.MainModule.MainScreenModule.Shared.Signals
{
    public class MainScreenSignals : ISignalHolder
    {
        public MainScreenSignalsIncoming Incoming = new();
        public MainScreenSignalsOutgoing Outgoing = new();
    }

    public class MainScreenSignalsIncoming
    {
        public Signal OpenMainScreen = new();
    }

    public class MainScreenSignalsOutgoing
    {
        public Signal<DifficultyType> DifficultySelected = new();
    }
}