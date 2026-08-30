using Modules.GameplayModule.GameplayScreenModule.Shared.Signals;
using Modules.GameplayModule.GameplayScreenModule.ViewsMediators;
#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;

namespace Modules.GameplayModule.GameplayScreenModule.GameplayScreenTestModule.RootsContexts
{
    [ExcludeFromContextWindow]
    public class GameplayScreenTestContext : BaseScreenContext
    {

		private GameplayScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<GameplayScreenSignals>();
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

        public override void Launch()
        {
            base.Launch();
			_screenService.Open<GameplayScreenView>().Show();

        }
    }
}
#endif
