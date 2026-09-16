#if UNITY_EDITOR

using FlowIoC.BaseModule.Contexts;
using Modules.AdsModule.AdsTestModule.Controllers;
using Modules.AdsModule.AdsTestModule.Models;
using Modules.AdsModule.AdsTestModule.Signals;
using Modules.AdsModule.AdsTestModule.ViewsMediators;
using Modules.AdsModule.Data.ValueObjects;
using Modules.AdsModule.Services;
using Modules.AdsModule.Signals;

namespace Modules.AdsModule.AdsTestModule.RootsContexts
{
    /// <summary>
    /// Every button is a flow that ends by re-reading the state, so the label always shows what
    /// the service knows. The two shows are the shipped steps bound the way a game binds them:
    /// the rewarded one with a grant behind it that only prints, so a dismissed ad is seen as a
    /// grant that did not print; the interstitial one with the result noted behind it.
    /// </summary>
    public class AdsTestContext : Context
    {
        private AdsTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<AdsTestInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<AdsTestModel>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<AdsTestView>().To<AdsTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.AnswerProvider)
                .ToSequence<AnswerProviderCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.ShowRewarded)
                .ToSequence<IAdsService.Commands.ShowRewarded>("chest")
                .ToSequence<ReportRewardGrantedCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.ShowInterstitial)
                .ToSequence<IAdsService.Commands.ShowInterstitial>("level_end")
                .ToSequence<NoteResultCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.RewardAndClose)
                .ToSequence<RewardAndCloseCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.Close)
                .ToSequence<CloseAdCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.FailToDisplay)
                .ToSequence<FailToDisplayCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.PayRevenue)
                .ToSequence<PayRevenueCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.SetLoadsFail)
                .ToSequence<SetLoadsFailCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.SetSilentShow)
                .ToSequence<SetSilentShowCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.SetAdsRemoved)
                .ToSequence<SetTestAdsRemovedCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.ConsentAll)
                .ToSequence<IAdsService.Commands.SetConsent>(new AdsConsentVO(true))
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.Initialize)
                .ToSequence<IAdsService.Commands.Initialize>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.OutgoingHeard)
                .ToSequence<NoteOutgoingCommand>()
                .ToSequence<ReportAdsStateCommand>();

            CommandBinder.Bind(_signals.ReportState).ToSequence<ReportAdsStateCommand>();
        }

        // A test-only liberty: the test context hears the module's Outgoing here, the one phase
        // that may reach across modules, so the label can show the last announcement.
        public override void Setup()
        {
            base.Setup();

            AdsSignals ads = InjectionBinderCrossContext.GetInstance<AdsSignals>();

            if (ads == null)
                return;

            ads.Outgoing.ReadyChanged.AddListener((format, ready) => _signals.OutgoingHeard.Dispatch($"ReadyChanged {format} {ready}"));
            ads.Outgoing.Opened.AddListener((format, placement) => _signals.OutgoingHeard.Dispatch($"Opened {format}/{placement}"));
            ads.Outgoing.Closed.AddListener((format, placement) => _signals.OutgoingHeard.Dispatch($"Closed {format}/{placement}"));
            ads.Outgoing.Failed.AddListener((format, placement, reason) => _signals.OutgoingHeard.Dispatch($"Failed {format}/{placement}: {reason}"));
            ads.Outgoing.RevenuePaid.AddListener(revenue => _signals.OutgoingHeard.Dispatch("RevenuePaid " + revenue));
        }

        // The view may register before this Root's Setup or after its Launch, depending on the
        // frame the injector lands in, so the state is reported from both places.
        public override void Launch()
        {
            base.Launch();

            _signals.ReportState.Dispatch();
        }
    }
}

#endif