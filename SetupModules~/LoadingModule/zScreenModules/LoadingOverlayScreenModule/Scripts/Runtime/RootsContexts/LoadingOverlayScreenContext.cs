using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.LoadingModule.LoadingOverlayScreenModule.Controllers;
using Modules.LoadingModule.LoadingOverlayScreenModule.Models;
using Modules.LoadingModule.LoadingOverlayScreenModule.Signals;
using Modules.LoadingModule.LoadingOverlayScreenModule.ViewsMediators;
using Modules.LoadingModule.Shared.Constants;

namespace Modules.LoadingModule.LoadingOverlayScreenModule.RootsContexts
{
    public class LoadingOverlayScreenContext : ScreenSubContext<LoadingOverlayScreenView, LoadingOverlayScreenMediator>
    {
        private LoadingOverlayScreenSignals _signals;

        /// <summary>
        /// Layer 8: under the fullscreen bar on 9, over everything a feature opens below. Resources
        /// like the fullscreen screen: both carry the tag the boot loads before its first set, and
        /// one addressable screen in that pair would hold the boot on Addressables' own initialisation
        /// with nothing on stage.
        /// </summary>
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 8,
            Tag = LoadingConstants.SCREEN_TAG,
            Load = ScreenLoadCVO.Resource("LoadingOverlayScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false
        };

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<LoadingOverlayScreenSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<ILoadingOverlayScreenModel, LoadingOverlayScreenModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.Open).ToSequence<OpenLoadingOverlayScreenCommand>();

            // What arrives is remembered first, so an overlay that finishes loading later is filled
            // from it; the Mediator applies the same signals to an overlay that is already up.
            CommandBinder.Bind(_signals.Incoming.Apply).ToSequence<RememberOverlayStatusCommand>();
            CommandBinder.Bind(_signals.Incoming.Close).ToSequence<RememberOverlayClosedCommand>();
        }
    }
}
