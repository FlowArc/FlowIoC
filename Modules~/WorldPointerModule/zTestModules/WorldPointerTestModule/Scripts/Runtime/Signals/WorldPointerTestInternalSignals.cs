#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;
using UnityEngine;

namespace Modules.WorldPointerModule.WorldPointerTestModule.Signals
{
    /// <summary>
    /// What the test module says to its own commands, and what they say back to the one view.
    /// A Mediator may inject nothing but its View, so the buttons leave as signals and the count
    /// comes back as one - the same shape as any module, at test size.
    /// </summary>
    public class WorldPointerTestInternalSignals : ISignalHolder
    {
        /// <summary>Open the sample screen - the displays - once the scene is up.</summary>
        public Signal OpenSampleScreen = new();

        /// <summary>Close the sample screen when it is open, open it when it is not.</summary>
        public Signal ToggleSampleScreen = new();

        /// <summary>Register every cube under its own id, with a first label.</summary>
        public Signal<Transform[]> RegisterPointers = new();

        /// <summary>Send every cube a new label.</summary>
        public Signal<Transform[]> ChangeContent = new();

        /// <summary>Hide every cube's pointer, or show them again.</summary>
        public Signal<Transform[], bool> SetHidden = new();

        /// <summary>Unregister every target.</summary>
        public Signal UnregisterPointers = new();

        /// <summary>How many targets the service holds now.</summary>
        public Signal<int> PointerCountChanged = new();
    }
}
#endif