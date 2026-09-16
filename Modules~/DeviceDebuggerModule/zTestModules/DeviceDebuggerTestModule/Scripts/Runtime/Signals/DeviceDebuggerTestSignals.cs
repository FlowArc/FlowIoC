#if UNITY_EDITOR

using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Signals;
using Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Enums;

namespace Modules.DeviceDebuggerModule.DeviceDebuggerTestModule.Signals
{
    /// <summary>
    /// A holder with every kind of option the panel draws, so the test scene shows each one. A
    /// test module has no Scripts/Signals folder, and this holder is bound across contexts
    /// anyway - a test module's liberty - because the panel only sees cross-context holders.
    /// </summary>
    public class DeviceDebuggerTestSignals : ISignalHolder
    {
        public DeviceDebuggerTestSignalsIncoming Incoming = new();
        public DeviceDebuggerTestSignalsOutgoing Outgoing = new();
    }

    public class DeviceDebuggerTestSignalsIncoming
    {
        [DebugOption("Sample", "Log a line", Order = 0)] public Signal LogLine = new();
        [DebugOption("Sample", "God mode", Order = 1)] public Signal<bool> SetGodMode = new();
        [DebugOption("Sample", "Coins", Order = 2, Min = 0, Max = 10)] public Signal<int> SetCoins = new();
        [DebugOption("Sample", "Speed", Order = 3)] public Signal<float> SetSpeed = new();
        [DebugOption("Sample", "Name", Order = 4)] public Signal<string> SetName = new();
        [DebugOption("Sample", "Mood", Order = 5)] public Signal<TestMood> SetMood = new();
        [DebugOption("Sample", "+1000 coins", Order = 6, Argument = 1000)] public Signal<int> AddCoins = new();
        [DebugOption("Trouble", "Throw an exception")] public Signal Throw = new();
        [DebugOption("Trouble", "Log 200 lines")] public Signal Spam = new();
        public Signal<int, int> Pair = new();
    }

    public class DeviceDebuggerTestSignalsOutgoing
    {
        [DebugOption("Sample", "God mode")] public Signal<bool> GodModeChanged = new();
        [DebugOption("Sample", "Coins now")] public Signal<int> CoinsChanged = new();
    }
}

#endif
