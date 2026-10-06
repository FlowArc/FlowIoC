#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Signals
{
    /// <summary>
    /// What the screen's Mediator says to its own commands: a press on a lane's Fly button, with
    /// the amount. A Mediator injects nothing but its View, so flying is the Commands'.
    /// </summary>
    internal class ResourceFlySampleScreenInternalSignals : ISignalHolder
    {
        public Signal<int> FlyScatter = new();
        public Signal<int> FlyDirect = new();
        public Signal<int> FlyCurved = new();
    }
}
#endif
