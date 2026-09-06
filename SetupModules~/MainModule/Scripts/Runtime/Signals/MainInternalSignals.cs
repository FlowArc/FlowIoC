using FlowIoC.BaseModule.Signals;

namespace Modules.MainModule.Signals
{
    /// <summary>
    /// What MainModule says to its own commands. It is dispatched by MainContext.Launch and handled
    /// by MainContext, so it crosses no boundary and has no Incoming or Outgoing - those two halves
    /// describe one, and an internal signal has none to describe.
    /// </summary>
    internal class MainInternalSignals : ISignalHolder
    {
        public Signal Launch = new Signal();
    }
}
