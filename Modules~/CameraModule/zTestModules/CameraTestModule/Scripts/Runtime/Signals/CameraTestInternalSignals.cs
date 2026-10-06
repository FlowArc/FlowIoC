#if UNITY_EDITOR

using FlowIoC.BaseModule.Signals;

namespace Modules.CameraModule.CameraTestModule.Signals
{
    /// <summary>The scene talks to itself: the buttons dispatch, the commands answer, the mediator listens.</summary>
    internal class CameraTestInternalSignals : ISignalHolder
    {
        public Signal Prepare = new();
        public Signal SwitchRequested = new();
        public Signal MoveRequested = new();
        public Signal ZoomRequested = new();
        public Signal<string> StateChanged = new();
    }
}

#endif
