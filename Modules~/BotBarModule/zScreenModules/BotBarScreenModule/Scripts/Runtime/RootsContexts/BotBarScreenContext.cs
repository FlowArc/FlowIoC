using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.BotBarModule.BotBarScreenModule.Controllers;
using Modules.BotBarModule.BotBarScreenModule.Signals;
using Modules.BotBarModule.BotBarScreenModule.ViewsMediators;

namespace Modules.BotBarModule.BotBarScreenModule.RootsContexts
{
    /// <summary>
    /// Layer 2 sits above the hub pages on 0 and the Gameplay screen on 1; a game whose layout
    /// differs overrides it on the Root's entry with Override Screen. The show animation is the
    /// slide in and the hide animation the slide out.
    /// </summary>
    public class BotBarScreenContext : ScreenSubContext<BotBarScreenView, BotBarScreenMediator>
    {
        private BotBarScreenSignals _signals;

        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("BotBarScreen"),
            HasShowAnimation = true,
            HasHideAnimation = true,
        };

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinderCrossContext.Bind<BotBarScreenSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.Open).ToSequence<OpenBotBarScreenCommand>();
        }
    }
}