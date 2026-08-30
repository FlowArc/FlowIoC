using FlowIoC.ScreenModule.RootsContexts;
using Modules.MainModule.MainScreenModule.Controllers;
using Modules.MainModule.MainScreenModule.Shared.Signals;

namespace Modules.MainModule.MainScreenModule.RootsContexts
{
    public class MainScreenContext : BaseScreenContext
    {
        private MainScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<MainScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.OpenMainScreen).ToSequence<OpenMainScreenCommand>();

        }
    }
}
