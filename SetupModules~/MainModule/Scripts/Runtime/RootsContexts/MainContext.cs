using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.Commands;
using Modules.MainModule.Controllers;
using Modules.MainModule.Signals;

namespace Modules.MainModule.RootsContexts
{
    public class MainContext : Context
    {
        private MainSignals _mainSignals;
        private MainInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _mainSignals = InjectionBinderCrossContext.Bind<MainSignals>();
            _internalSignals = InjectionBinderCrossContext.Bind<MainInternalSignals>();
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

            CommandBinder.Bind(_internalSignals.Launch)
                .ToSequence<LogStartupCommand>()
                .ToSequence<DispatchSignalCommand>(_mainSignals.Outgoing.Started);
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
            _internalSignals.Launch.Dispatch();
        }
    }
}