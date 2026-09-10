using FlowIoC.BaseModule.Contexts;
using Modules.AbTestFlowModule.Controllers;
using Modules.AbTestFlowModule.Models;
using Modules.AbTestFlowModule.Services;
using Modules.AbTestFlowModule.Signals;

namespace Modules.AbTestFlowModule.RootsContexts
{
    public class AbTestFlowServiceContext : Context
    {
        private AbTestFlowSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinderCrossContext.Bind<AbTestFlowSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<IAbTestFlowModel, AbTestFlowModel>();

            // The service is the one thing bound cross-context: it is the module's single
            // counterpart, and whatever anyone outside needs - the Root putting the Editor's assets
            // back on quit included - goes through a method on it. Its PostConstruct starts the flow
            // that reads the model, and the model's own runs first: the local binder's PostConstructs
            // run before the cross-context binder's.
            InjectionBinderCrossContext.Bind<IAbTestFlowService, AbTestFlowService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.ResolveAbTests)
                .ToSequence<ReadStoredAbTestsCommand>()
                .ToSequence<ProcessAbTestCommand>()
                .ToSequence<ApplyOverridesCommand>();
        }
    }
}