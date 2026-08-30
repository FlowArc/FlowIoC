using Modules.MainModule.MainScreenModule.Shared.Signals;
using Modules.MainModule.MainScreenModule.ViewsMediators;
#if UNITY_EDITOR
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.RootsContexts;

namespace Modules.MainModule.MainScreenModule.MainScreenTestModule.RootsContexts
{
    [ExcludeFromContextWindow]
    public class MainScreenTestContext : BaseScreenContext
    {

		private MainScreenSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
			_signals = InjectionBinderCrossContext.Bind<MainScreenSignals>();
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
			_screenService.Open<MainScreenView>().Show();

        }
    }
}
#endif
