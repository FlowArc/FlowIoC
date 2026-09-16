#if UNITY_EDITOR

using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ConsoleModule;
using Modules.AnalyticsModule.AnalyticsTestModule.Services;
using Modules.AnalyticsModule.Services;

namespace Modules.AnalyticsModule.AnalyticsTestModule.RootsContexts
{
    /// <summary>
    /// The test scene's plug, listed on AnalyticsServiceRoot the way a real plug is. The provider
    /// is bound across contexts as well, which a real plug never does: the test Root's buttons
    /// answer its held callback and its label reads what it recorded.
    /// </summary>
    [AllowAsSubContext]
    public class RecordingAnalyticsContext : Context
    {
        private IAnalyticsService _analytics;
        private RecordingAnalyticsProvider _provider;

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            _provider = InjectionBinderCrossContext.Bind<RecordingAnalyticsProvider>();
        }

        public override void Setup()
        {
            base.Setup();

            _analytics = InjectionBinderCrossContext.GetInstance<IAnalyticsService>();

            if (_analytics == null)
            {
                FlowLogger.LogError("Setup - IAnalyticsService is not bound: list this context on AnalyticsServiceRoot.");
                return;
            }

            _analytics.Plug(_provider);
        }

        public override void DestroyContext()
        {
            if (_provider != null)
                _analytics?.Unplug(_provider);

            base.DestroyContext();
        }
    }
}

#endif
