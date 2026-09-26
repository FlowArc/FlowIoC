#if UNITY_EDITOR

using FlowIoC.BaseModule.Contexts;
using Modules.LocalSaveModule.LocalSaveTestModule.Controllers;
using Modules.LocalSaveModule.LocalSaveTestModule.Data.UnityObjects;
using Modules.LocalSaveModule.LocalSaveTestModule.Models;
using Modules.LocalSaveModule.LocalSaveTestModule.Signals;
using Modules.LocalSaveModule.LocalSaveTestModule.ViewsMediators;
using Modules.LocalSaveModule.Services;

namespace Modules.LocalSaveModule.LocalSaveTestModule.RootsContexts
{
    public class LocalSaveTestContext : Context
    {
        private LocalSaveTestSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinderCrossContext.Bind<LocalSaveTestSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<ILocalSaveTestModel, LocalSaveTestModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<LocalSaveTestView>().To<LocalSaveTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.ReportProbe).ToSequence<ReportProbeCommand>();

            CommandBinder.Bind(_signals.Incoming.IncrementProbe)
                .ToSequence<IncrementProbeCommand>()
                .ToSequence<ILocalSaveService.Commands.Save>(nameof(SD_LocalSaveProbe))
                .ToSequence<ReportProbeCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            _signals.Incoming.ReportProbe.Dispatch();
        }
    }
}

#endif