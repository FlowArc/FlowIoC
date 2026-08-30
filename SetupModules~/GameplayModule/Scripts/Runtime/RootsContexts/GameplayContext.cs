using Modules.GameplayModule.Shared.Signals;
using FlowIoC.BaseModule.Contexts;

namespace Modules.GameplayModule.RootsContexts
{

    public class GameplayContext : Context
    {
		private GameplaySignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<GameplaySignals>();
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
        }

        public override void Setup()
        {
            base.Setup();
        }

        public override void Launch()
        {
            base.Launch();
        }
    }
}
