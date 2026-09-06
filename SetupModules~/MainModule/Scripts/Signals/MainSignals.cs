using FlowIoC.BaseModule.Signals;

namespace Modules.MainModule.Signals
{
    public class MainSignals : ISignalHolder
    {
        public MainSignalsIncoming Incoming = new();
        public MainSignalsOutgoing Outgoing = new();

        /// <summary>
        /// Empty, and honestly so: MainModule is the application's entry point and is told nothing.
        /// It starts itself in Launch and announces that it has.
        /// </summary>
        public class MainSignalsIncoming
        {
        }

        public class MainSignalsOutgoing
        {
            /// <summary>
            /// The application has come up. An announcement rather than an order - what should
            /// follow it is the Connector's to join to somebody's Incoming, and MainModule does not
            /// decide that opening the main screen is what starting means.
            /// </summary>
            public Signal Started = new();
        }
    }
}
