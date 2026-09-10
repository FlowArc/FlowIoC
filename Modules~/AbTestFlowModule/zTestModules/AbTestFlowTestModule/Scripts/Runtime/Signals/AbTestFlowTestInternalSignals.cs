#if UNITY_EDITOR

using FlowIoC.BaseModule.Signals;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Data.ValueObjects;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Signals
{
    /// <summary>
    /// The scene talks to itself and to nobody else, so every signal here is internal: the buttons
    /// dispatch, the commands answer, and the mediator listens.
    /// </summary>
    public class AbTestFlowTestInternalSignals : ISignalHolder
    {
        /// <summary>Shows the experiment and the probe as they stand, without changing anything.</summary>
        public Signal ReportState = new();

        /// <summary>Forgets this player's assignment, so the next roll is a fresh one.</summary>
        public Signal ClearStoredAbTest = new();

        /// <summary>What a designer does to restart the experiment: raises its version by one.</summary>
        public Signal RaiseVersion = new();

        /// <summary>Runs the boot decision again, so a change shows without leaving play mode.</summary>
        public Signal Reroll = new();

        public Signal<AbTestFlowTestStateVO> StateChanged = new();
    }
}

#endif
