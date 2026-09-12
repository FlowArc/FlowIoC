using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.LoadingModule.LoadingScreenModule.Controllers;
using Modules.LoadingModule.LoadingScreenModule.Models;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.LoadingModule.LoadingScreenModule.ViewsMediators;

namespace Modules.LoadingModule.LoadingScreenModule.RootsContexts
{
    public class LoadingScreenContext : ScreenSubContext<LoadingScreenView, LoadingScreenMediator>
    {
        private LoadingScreenSignals _signals;

        /// <summary>Layer 9 is the top of the shipped ScreenManager, so nothing the boot opens sits over the bar.</summary>
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 9,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("LoadingScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<LoadingScreenSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<ILoadingScreenModel, LoadingScreenModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.Open).ToSequence<OpenLoadingScreenCommand>();

            // What arrives is remembered first, so a screen that finishes loading later is filled
            // from it; the Mediator applies the same signals to a screen that is already up.
            CommandBinder.Bind(_signals.Incoming.Apply).ToSequence<RememberStatusCommand>();
            CommandBinder.Bind(_signals.Incoming.Close).ToSequence<RememberClosedCommand>();
            CommandBinder.Bind(_signals.Incoming.ShowFailed).ToSequence<RememberFailedCommand>();
        }
    }
}