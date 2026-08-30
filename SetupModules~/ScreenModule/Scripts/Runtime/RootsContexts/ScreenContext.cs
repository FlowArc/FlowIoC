using Modules.ScreenModule.Shared.Signals;
using FlowIoC.ScreenModule.RootsContexts;

namespace Modules.ScreenModule.RootsContexts
{

    public class ScreenContext : BaseScreenContext
    {
		private ScreenSignals _screenSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _screenSignals = InjectionBinderCrossContext.Bind<ScreenSignals>();
        }


        public override void CommandBindings()
        {
            base.CommandBindings();
        }

    }
}
