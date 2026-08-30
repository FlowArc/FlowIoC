using FlowIoC.BaseModule.Signals;

namespace FlowIoC.Editor.CodeGenerator.TempSignals
{
    /// <summary>
    /// What the module says to its own commands. None of these leave the module, so they sit apart
    /// from the public holder in Shared rather than widening what the module offers.
    ///
    /// There is no Incoming and no Outgoing here. Those two halves say what a module accepts and
    /// what it announces across a boundary, and an internal signal never crosses one.
    /// </summary>
    internal class TempInternalSignals : ISignalHolder
    {
    }
}
