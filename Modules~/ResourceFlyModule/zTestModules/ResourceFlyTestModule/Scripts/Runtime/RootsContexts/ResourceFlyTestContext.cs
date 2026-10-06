#if UNITY_EDITOR
using FlowIoC.ScreenModule.RootsContexts;
using Modules.ResourceFlyModule.ResourceFlyTestModule.Controllers;
using Modules.ResourceFlyModule.ResourceFlyTestModule.Signals;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.RootsContexts
{
    /// <summary>
    /// ResourceFly's sample scene: it opens the sample screen, listed on this Root beside it, and
    /// the screen does the rest. The ScreenManager hangs under this Root, so the context is a
    /// BaseScreenContext, which mediates it.
    /// </summary>
    public class ResourceFlyTestContext : BaseScreenContext
    {
        private ResourceFlyTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<ResourceFlyTestInternalSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.OpenSampleScreen).ToSequence<OpenSampleScreenCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            _signals.OpenSampleScreen.Dispatch();
        }
    }
}
#endif
