#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;
using Modules.WorldPointerModule.Entities;
using UnityEngine;

namespace Modules.WorldPointerModule.WorldPointerTestModule.Signals
{
    /// <summary>
    /// What the test module says to its own commands, and what they say back to the one view.
    /// A Mediator may inject nothing but its View, so the two buttons leave as signals and the
    /// count comes back as one - the same shape as any module, at test size.
    /// </summary>
    public class WorldPointerTestInternalSignals : ISignalHolder
    {
        /// <summary>Register every cube with the indicator at the same index.</summary>
        public Signal<Transform[], WorldPointerIndicator[]> RegisterPointers = new();

        /// <summary>Stop every pointer.</summary>
        public Signal UnregisterPointers = new();

        /// <summary>How many pointers the service is following now.</summary>
        public Signal<int> PointerCountChanged = new();
    }
}
#endif
