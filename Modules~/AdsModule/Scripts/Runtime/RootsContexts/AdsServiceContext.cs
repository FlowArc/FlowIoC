using FlowIoC.BaseModule.Contexts;
using Modules.AdsModule.Controllers;
using Modules.AdsModule.Models;
using Modules.AdsModule.Services;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.RootsContexts
{
    /// <summary>
    /// Declares the module and nothing else. The plug plugs itself in during Setup - from its own
    /// hosted context, listed on this Root - and Launch asks whether to initialize; from then on
    /// each call the service takes, and each report the provider makes, reads below in the order
    /// it runs.
    /// </summary>
    public class AdsServiceContext : Context
    {
        private AdsInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            InjectionBinderCrossContext.Bind<AdsSignals>();
            _signals = InjectionBinder.Bind<AdsInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<IAdsModel, AdsModel>();
            InjectionBinder.Bind<IAdsProviderListener, AdsProviderListener>();

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<IAdsService, AdsService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Plug)
                .ToSequence<PlugProviderCommand>()
                .ToSequence<InitializeProviderCommand>();

            CommandBinder.Bind(_signals.Unplug).ToSequence<UnplugProviderCommand>();

            CommandBinder.Bind(_signals.Launched)
                .ToSequence<RequestInitializeOnLaunchCommand>()
                .ToSequence<InitializeProviderCommand>();

            CommandBinder.Bind(_signals.Initialize)
                .ToSequence<RequestInitializeCommand>()
                .ToSequence<InitializeProviderCommand>();

            CommandBinder.Bind(_signals.ProviderInitialized).ToSequence<MarkProviderReadyCommand>();

            CommandBinder.Bind(_signals.Load).ToSequence<LoadAdCommand>();

            CommandBinder.Bind(_signals.Loaded).ToSequence<MarkAdReadyCommand>();

            CommandBinder.Bind(_signals.LoadFailed).ToSequence<RetryLoadCommand>();

            CommandBinder.Bind(_signals.Show).ToSequence<ShowAdCommand>();

            CommandBinder.Bind(_signals.Displayed).ToSequence<AnnounceAdOpenedCommand>();

            CommandBinder.Bind(_signals.DisplayFailed).ToSequence<FailShowCommand>();

            CommandBinder.Bind(_signals.RewardEarned).ToSequence<RecordRewardCommand>();

            CommandBinder.Bind(_signals.Closed).ToSequence<FinishShowCommand>();

            CommandBinder.Bind(_signals.RevenuePaid).ToSequence<AnnounceRevenueCommand>();

            CommandBinder.Bind(_signals.SetConsent).ToSequence<SetConsentCommand>();

            CommandBinder.Bind(_signals.SetAdsRemoved).ToSequence<SetAdsRemovedCommand>();

            CommandBinder.Bind(_signals.SetMuted).ToSequence<SetMutedCommand>();

            CommandBinder.Bind(_signals.ShowDebugger).ToSequence<ShowDebuggerCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            // Every Setup in the scene has run, so the plug that lives here has plugged.
            _signals.Launched.Dispatch();
        }
    }
}