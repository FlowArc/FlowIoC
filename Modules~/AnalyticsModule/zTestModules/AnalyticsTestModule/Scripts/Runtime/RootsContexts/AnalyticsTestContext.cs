#if UNITY_EDITOR

using FlowIoC.BaseModule.Contexts;
using Modules.AnalyticsModule.AnalyticsTestModule.Controllers;
using Modules.AnalyticsModule.AnalyticsTestModule.Signals;
using Modules.AnalyticsModule.AnalyticsTestModule.ViewsMediators;
using Modules.AnalyticsModule.Data.ValueObjects;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.AnalyticsTestModule.RootsContexts
{
    /// <summary>
    /// Every button is a flow that ends by re-reading the state, so the label always shows what
    /// the service knows. The fixed event, the property and the two consents are the shipped
    /// steps, bound the way a game binds them; the event built from state and the user id are
    /// Commands, the way a game writes them.
    /// </summary>
    public class AnalyticsTestContext : Context
    {
        private AnalyticsTestInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<AnalyticsTestInternalSignals>();
        }

        public override void MediationBindings()
        {
            base.MediationBindings();

            MediationBinder.Bind<AnalyticsTestView>().To<AnalyticsTestMediator>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.LogTestEvent)
                .ToSequence<LogTestEventCommand>()
                .ToSequence<ReportAnalyticsStateCommand>();

            CommandBinder.Bind(_signals.LogStep)
                .ToSequence<IAnalyticsService.Commands.Log>(new AnalyticsEventVO("screen_open").With("screen", "test"))
                .ToSequence<ReportAnalyticsStateCommand>();

            CommandBinder.Bind(_signals.SetProperty)
                .ToSequence<IAnalyticsService.Commands.SetUserProperty>("tier", "gold")
                .ToSequence<ReportAnalyticsStateCommand>();

            CommandBinder.Bind(_signals.SetId)
                .ToSequence<SetTestUserIdCommand>()
                .ToSequence<ReportAnalyticsStateCommand>();

            CommandBinder.Bind(_signals.ConsentGranted)
                .ToSequence<IAnalyticsService.Commands.SetConsent>(new AnalyticsConsentVO(true))
                .ToSequence<ReportAnalyticsStateCommand>();

            CommandBinder.Bind(_signals.ConsentDenied)
                .ToSequence<IAnalyticsService.Commands.SetConsent>(new AnalyticsConsentVO(false))
                .ToSequence<ReportAnalyticsStateCommand>();

            CommandBinder.Bind(_signals.AnswerProvider)
                .ToSequence<AnswerProviderCommand>()
                .ToSequence<ReportAnalyticsStateCommand>();

            CommandBinder.Bind(_signals.ReportState).ToSequence<ReportAnalyticsStateCommand>();
        }

        // The view may register before this Root's Setup or after its Launch, depending on the
        // frame the injector lands in, so the state is reported from both places: here, once the
        // service has launched, and from the mediator when the view registers.
        public override void Launch()
        {
            base.Launch();

            _signals.ReportState.Dispatch();
        }
    }
}

#endif