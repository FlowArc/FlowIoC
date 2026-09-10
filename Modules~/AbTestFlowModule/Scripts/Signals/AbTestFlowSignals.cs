using FlowIoC.BaseModule.Signals;

namespace Modules.AbTestFlowModule.Signals
{
    public class AbTestFlowSignals : ISignalHolder
    {
        public AbTestFlowSignalsIncoming Incoming = new();
        public AbTestFlowSignalsOutgoing Outgoing = new();
    }

    public class AbTestFlowSignalsIncoming
    {
        /// <summary>
        /// Decides every active experiment for this player and writes the assigned groups' variants
        /// over the originals. The service dispatches it from its PostConstruct, during the binding
        /// pass, so the overrides have landed before any other module reads its config. Dispatching
        /// it again decides again: a stored assignment stands, so the same version answers the same
        /// group.
        /// </summary>
        public Signal ResolveAbTests = new();
    }

    /// <summary>
    /// Nothing is announced. What was decided is decided at boot, before any other module is
    /// listening, so a signal would reach nobody: the status is published in RD_AbTestStatus
    /// instead, and a module that wants to know reads it when it is ready.
    /// </summary>
    public class AbTestFlowSignalsOutgoing
    {
    }
}