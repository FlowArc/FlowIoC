using FlowIoC.BaseModule.Signals;

namespace FlowIoC.Editor.CodeGenerator.TempSignals
{
    internal class TempSignals : ISignalHolder
    {
        public TempSignalsIncoming Incoming = new();
        public TempSignalsOutgoing Outgoing = new();
    }

    internal class TempSignalsIncoming
    {
    }

    internal class TempSignalsOutgoing
    {
    }
}
