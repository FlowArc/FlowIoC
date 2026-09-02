using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.MainModule.MainScreenModule.Controllers;
using Modules.MainModule.MainScreenModule.Shared.Signals;
using Modules.MainModule.MainScreenModule.ViewsMediators;

namespace Modules.MainModule.MainScreenModule.RootsContexts
{
    public class MainScreenContext : ScreenSubContext<MainScreenView, MainScreenMediator>
    {
        private MainScreenSignals _signals;

        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 0,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("MainScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<MainScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.OpenMainScreen).ToSequence<OpenMainScreenCommand>();
        }
    }
}