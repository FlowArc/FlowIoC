#if UNITY_EDITOR

using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AbTestFlowModule.Services;
using Modules.AbTestFlowModule.Signals;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.Controllers
{
    /// <summary>
    /// Runs the boot decision again without leaving play mode: the same signal the service
    /// dispatches from its PostConstruct, after the originals are put back - or the new group's
    /// variant would land on top of the old one's. The service is the module's one counterpart,
    /// so putting them back is asked of it.
    ///
    /// A test module may reference anything, which is what lets it reach the module's signals
    /// directly rather than through a Connector; an ordinary module never would.
    /// </summary>
    public class RerollCommand : Command
    {
        [Inject] private IAbTestFlowService _abTests { get; set; }

        [InjectSignal] private AbTestFlowSignals _abTestSignals { get; set; }

        public override void Execute()
        {
            _abTests.RestoreEditorAssets();
            _abTestSignals.Incoming.ResolveAbTests.Dispatch();
        }
    }
}

#endif