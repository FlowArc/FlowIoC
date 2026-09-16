using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.FirebaseAnalyticsModule.Services;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.FirebaseAnalyticsModule.RootsContexts
{
    /// <summary>
    /// The Firebase plug. Listed on AnalyticsServiceRoot's Sub Context Types; its Setup - the one
    /// phase that may reach across modules - gets the service and plugs the provider. The Root
    /// beside it is for a game that prefers the plug on a Root of its own.
    /// </summary>
    [AllowAsSubContext]
    public class FirebaseAnalyticsServiceContext : Context
    {
        private IAnalyticsService _analytics;
        private FirebaseAnalyticsProvider _provider;

        public override void Setup()
        {
            base.Setup();

            _analytics = InjectionBinderCrossContext.GetInstance<IAnalyticsService>();

            if (_analytics == null)
            {
                FlowLogger.LogError(
                    "Setup - IAnalyticsService is not bound: list this context on AnalyticsServiceRoot, or put that Root in the scene.");
                return;
            }

            _provider = new FirebaseAnalyticsProvider();
            _analytics.Plug(_provider);
        }

        public override void DestroyContext()
        {
            // The service reference is kept from Setup: at teardown the host's own context is
            // destroyed before its sub-contexts, so its cross-context bindings may already be gone.
            if (_provider != null)
                _analytics?.Unplug(_provider);

            base.DestroyContext();
        }
    }
}