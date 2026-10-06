#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.Signals
{
    /// <summary>What the test module says to its own commands.</summary>
    internal class ResourceFlyTestInternalSignals : ISignalHolder
    {
        public Signal OpenSampleScreen = new();
    }
}
#endif
