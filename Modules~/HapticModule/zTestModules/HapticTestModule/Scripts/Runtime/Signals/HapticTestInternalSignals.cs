#if UNITY_EDITOR

using FlowIoC.BaseModule.Signals;
using Modules.HapticModule.Enums;
using Modules.HapticModule.HapticTestModule.Data.ValueObjects;

namespace Modules.HapticModule.HapticTestModule.Signals
{
    /// <summary>The scene talks to itself: the buttons dispatch, the commands answer, the mediator listens.</summary>
    public class HapticTestInternalSignals : ISignalHolder
    {
        public Signal<HapticPreset> PlayRequested = new();
        public Signal<bool> EnabledChangeRequested = new();
        public Signal ReportState = new();
        public Signal<HapticTestStateVO> StateChanged = new();
    }
}

#endif
