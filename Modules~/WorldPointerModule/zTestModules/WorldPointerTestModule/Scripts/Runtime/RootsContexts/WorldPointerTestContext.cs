#if UNITY_EDITOR
using FlowIoC.BaseModule.Contexts;
using Modules.WorldPointerModule.WorldPointerTestModule.Controllers;
using Modules.WorldPointerModule.WorldPointerTestModule.Signals;
using Modules.WorldPointerModule.WorldPointerTestModule.ViewsMediators;

namespace Modules.WorldPointerModule.WorldPointerTestModule.RootsContexts
{
    public class WorldPointerTestContext : Context
    {
        private WorldPointerTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<WorldPointerTestInternalSignals>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<WorldPointerTestView>().To<WorldPointerTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.RegisterPointers).ToSequence<RegisterTestPointersCommand>();
            CommandBinder.Bind(_signals.UnregisterPointers).ToSequence<UnregisterTestPointersCommand>();
        }
    }
}
#endif
