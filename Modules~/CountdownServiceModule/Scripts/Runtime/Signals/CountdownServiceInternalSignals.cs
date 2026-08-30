using FlowIoC.BaseModule.Signals;
using Modules.CountdownServiceModule.Data.ValueObjects;

namespace Modules.CountdownServiceModule.Signals
{
    /// <summary>
    /// What the service says to its own commands. None of these leave the module, so they sit
    /// apart from the public holder rather than widening what the module offers.
    /// </summary>
    internal class CountdownServiceInternalSignals : ISignalHolder
    {
        /// <summary>One second of the module's clock. Hidden from the command log because it
        /// fires forever and would bury everything else in the Flow Console.</summary>
        public Signal Tick = new(hideCommandLog: true);

        public Signal<CountdownRequestVO> AddCountdownData = new();
        public Signal<CountdownRequestVO> AddCallbacks = new();
        public Signal<CountdownRequestVO> RemoveCallbacks = new();
        public Signal<string> StopCountdown = new();
    }
}
