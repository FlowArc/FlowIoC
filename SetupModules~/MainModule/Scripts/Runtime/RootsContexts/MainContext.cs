using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller.Commands;
using Modules.LoadingModule.Controllers;
using Modules.MainModule.Constants;
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
            _internalSignals = InjectionBinder.Bind<MainInternalSignals>();
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

            // The boot, read top to bottom. BootStarted is the fan-out for what may run beside the
            // boot without slowing it; Started is the fan-out for what waits until the player is in.
            CommandBinder.Bind(_internalSignals.Launch)
                .ToSequence<BeginLoadingCommand>(MainConstants.BOOT_SET)
                .ToSequence<DispatchSignalCommand>(_mainSignals.Outgoing.BootStarted)
                .ToParallel<PreloadScreensCommand>()
                .ToSequence<FillPoolsCommand>()
                .ToSequence<AwaitLoadingCommand>(MainConstants.BOOT_SET)
                .ToSequence<DispatchSignalCommand>(_mainSignals.Outgoing.Started);

            // A retry from the loading screen runs the boot again.
            CommandBinder.Bind(_mainSignals.Incoming.RetryBoot)
                .ToSequence<DispatchSignalCommand>(_internalSignals.Launch);
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