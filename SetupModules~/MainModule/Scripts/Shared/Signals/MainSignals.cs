using FlowIoC.BaseModule.Signals;

namespace Modules.MainModule.Shared.Signals
{
    public class MainSignals : ISignalHolder
    {
        public Signal Launch = new Signal();

        public MainSignalsIncoming Incoming = new ();
        public MainSignalsOutgoing Outgoing = new ();

        public class MainSignalsIncoming
        {
        }

        public class MainSignalsOutgoing
        {
            public Signal OpenMainScene = new ();
            
        }
    }
}
