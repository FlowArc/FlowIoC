using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.Commands;
using Modules.MainModule.Controllers;
using Modules.MainModule.Shared.Signals;

namespace Modules.MainModule.RootsContexts
{
   
    public class MainContext : Context
    {
        private MainSignals _mainSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _mainSignals = InjectionBinderCrossContext.Bind<MainSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_mainSignals.Launch)
                .ToSequence<LogStartupCommand>()
                .ToSequence<DispatchSignalCommand>(_mainSignals.Outgoing.OpenMainScene);
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
            _mainSignals.Launch.Dispatch();
        }
    }
}
