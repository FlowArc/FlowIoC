using FlowIoC.BaseModule.Contexts;
using Modules.AnalyticsModule.Controllers;
using Modules.AnalyticsModule.Models;
using Modules.AnalyticsModule.Services;
using Modules.AnalyticsModule.Signals;

namespace Modules.AnalyticsModule.RootsContexts
{
    /// <summary>
    /// Declares the module and nothing else. Providers plug themselves in during Setup - each
    /// from its own hosted context, listed on this Root - and Launch asks every plugged one to
    /// initialize; from then on each call the service takes reads below in the order it runs.
    /// </summary>
    public class AnalyticsServiceContext : Context
    {
        private AnalyticsInternalSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();

            _signals = InjectionBinder.Bind<AnalyticsInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinder.Bind<IAnalyticsModel, AnalyticsModel>();

            // The one type other modules reference directly, which is what makes this a Service.
            InjectionBinderCrossContext.Bind<IAnalyticsService, AnalyticsService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Plug)
                .ToSequence<PlugProviderCommand>()
                .ToSequence<InitializeProvidersCommand>();

            CommandBinder.Bind(_signals.Unplug).ToSequence<UnplugProviderCommand>();

            CommandBinder.Bind(_signals.Initialize)
                .ToSequence<MarkLaunchedCommand>()
                .ToSequence<InitializeProvidersCommand>();

            CommandBinder.Bind(_signals.ProviderReady).ToSequence<FlushProviderCommand>();

            CommandBinder.Bind(_signals.Log).ToSequence<LogEventCommand>();

            CommandBinder.Bind(_signals.SetUserProperty).ToSequence<SetUserPropertyCommand>();

            CommandBinder.Bind(_signals.SetUserId).ToSequence<SetUserIdCommand>();

            CommandBinder.Bind(_signals.SetConsent).ToSequence<SetConsentCommand>();
        }

        public override void Launch()
        {
            base.Launch();

            // Every Setup in the scene has run, so every provider that lives here has plugged.
            _signals.Initialize.Dispatch();
        }
    }
}