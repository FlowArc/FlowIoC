using FlowIoC.BaseModule.Signals;

namespace Modules.MainModule.Signals
{
    public class MainSignals : ISignalHolder
    {
        public MainSignalsIncoming Incoming = new();
        public MainSignalsOutgoing Outgoing = new();

        /// <summary>
        /// MainModule is the application's entry point and is told almost nothing. It starts itself
        /// in Launch and announces that it has; the one thing it accepts is being asked to boot again.
        /// </summary>
        public class MainSignalsIncoming
        {
            /// <summary>The loading screen's retry button, carried here by the Connector: run the boot again.</summary>
            public Signal RetryBoot = new();
        }

        public class MainSignalsOutgoing
        {
            /// <summary>
            /// The boot has begun and its screen is up. The fan-out for what may run beside the boot
            /// without slowing it - an SDK initialising, a profile fetch - reporting its own steps
            /// into whatever set CD_LoadingSets puts them in.
            /// </summary>
            public Signal BootStarted = new();

            /// <summary>
            /// The application has come up. An announcement rather than an order - what should
            /// follow it is the Connector's to join to somebody's Incoming, and MainModule does not
            /// decide that opening the main screen is what starting means.
            /// </summary>
            public Signal Started = new();
        }
    }
}