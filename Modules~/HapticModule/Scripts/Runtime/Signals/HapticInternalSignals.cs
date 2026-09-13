using FlowIoC.BaseModule.Signals;
using Modules.HapticModule.Enums;

namespace Modules.HapticModule.Signals
{
    /// <summary>
    /// What the service says to its own commands. Nothing here leaves the module: the module has
    /// no public holder at all, because a Service answers the caller it was given.
    /// </summary>
    internal class HapticInternalSignals : ISignalHolder
    {
        public Signal Initialize = new();
        public Signal<HapticPreset> Play = new();
        public Signal<bool> SetEnabled = new();
    }
}
