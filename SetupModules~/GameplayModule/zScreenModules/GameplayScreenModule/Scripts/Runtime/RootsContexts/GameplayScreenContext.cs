using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.GameplayScreenModule.Controllers;
using Modules.GameplayModule.GameplayScreenModule.Shared.Signals;

namespace Modules.GameplayModule.GameplayScreenModule.RootsContexts
{
    public class GameplayScreenContext : BaseScreenContext
    {
        private GameplayScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<GameplayScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.OpenGameplayScreen).ToSequence<OpenGameplayScreenCommand>();
        }
    }
}
