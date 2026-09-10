#if UNITY_EDITOR

using FlowIoC.BaseModule.Contexts;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Controllers;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Models;
using Modules.AbTestFlowModule.AbTestFlowTestModule.Signals;
using Modules.AbTestFlowModule.AbTestFlowTestModule.ViewsMediators;

namespace Modules.AbTestFlowModule.AbTestFlowTestModule.RootsContexts
{
    public class AbTestFlowTestContext : Context
    {
        private AbTestFlowTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinderCrossContext.Bind<AbTestFlowTestInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            // Local: nothing outside this context needs it. The Root reaches it through the
            // context's own binder on quit, to put the module's config back.
            InjectionBinder.Bind<IAbTestFlowTestModel, AbTestFlowTestModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<AbTestFlowTestView>().To<AbTestFlowTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.ReportState).ToSequence<ReportStateCommand>();

            CommandBinder.Bind(_signals.ClearStoredAbTest)
                .ToSequence<ClearStoredAbTestCommand>()
                .ToSequence<ReportStateCommand>();

            CommandBinder.Bind(_signals.RaiseVersion)
                .ToSequence<RaiseVersionCommand>()
                .ToSequence<ReportStateCommand>();

            CommandBinder.Bind(_signals.Reroll)
                .ToSequence<RerollCommand>()
                .ToSequence<ReportStateCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            _signals.ReportState.Dispatch();
        }
    }
}

#endif