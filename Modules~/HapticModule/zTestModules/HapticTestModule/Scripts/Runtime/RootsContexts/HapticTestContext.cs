#if UNITY_EDITOR

using FlowIoC.BaseModule.Contexts;
using Modules.HapticModule.HapticTestModule.Controllers;
using Modules.HapticModule.HapticTestModule.Signals;
using Modules.HapticModule.HapticTestModule.ViewsMediators;

namespace Modules.HapticModule.HapticTestModule.RootsContexts
{
    public class HapticTestContext : Context
    {
        private HapticTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<HapticTestInternalSignals>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<HapticTestView>().To<HapticTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.PlayRequested)
                .ToSequence<PlayTestHapticCommand>();

            CommandBinder.Bind(_signals.EnabledChangeRequested)
                .ToSequence<SetTestHapticsEnabledCommand>()
                .ToSequence<ReportHapticStateCommand>();

            CommandBinder.Bind(_signals.ReportState)
                .ToSequence<ReportHapticStateCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            _signals.ReportState.Dispatch();
        }
    }
}

#endif