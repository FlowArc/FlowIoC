using FlowIoC.BaseModule.Contexts;
using Modules.InputModule.Controllers;
using Modules.InputModule.Shared.Signals;
using Modules.InputModule.ViewsMediators;

namespace Modules.InputModule.RootsContexts
{
    public class InputContext : Context
    {
        private InputSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinderCrossContext.Bind<InputSignals>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<InputView>().To<InputMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.SetActionMapEnabled)
                .ToSequence<SetActionMapEnabledCommand>();
        }
    }
}
