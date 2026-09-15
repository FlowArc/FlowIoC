using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.LoadingModule.LoadingScreenModule.Controllers;
using Modules.LoadingModule.LoadingScreenModule.Models;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.LoadingModule.LoadingScreenModule.ViewsMediators;
using Modules.LoadingModule.Shared.Constants;

namespace Modules.LoadingModule.LoadingScreenModule.RootsContexts
{
    public class LoadingScreenContext : ScreenSubContext<LoadingScreenView, LoadingScreenMediator>
    {
        private LoadingScreenSignals _signals;
        private LoadingScreenInternalSignals _internalSignals;

        /// <summary>
        /// Layer 9 is the top of the shipped ScreenManager, so nothing the boot opens sits over the
        /// bar. Resources rather than Addressables: this screen shows the loads, so it cannot wait
        /// on Addressables' own initialisation - seconds on a remote catalogue, with nothing on
        /// stage. The art behind the bar is what comes from Addressables, loaded at Launch.
        /// </summary>
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 9,
            Tag = LoadingConstants.SCREEN_TAG,
            Load = ScreenLoadCVO.Resource("LoadingScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<LoadingScreenSignals>();
            _internalSignals = InjectionBinder.Bind<LoadingScreenInternalSignals>();
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

            CommandBinder.Bind(_internalSignals.LoadBackground).ToSequence<LoadBackgroundCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            _internalSignals.LoadBackground.Dispatch();
        }
    }
}
