#if UNITY_EDITOR

using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.AdsTestModule.Services;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.AdsTestModule.RootsContexts
{
    /// <summary>
    /// The test scene's plug, listed on AdsServiceRoot the way a real plug is. The provider is
    /// bound across contexts as well, which a real plug never does: the test Root's buttons
    /// answer it and its label reads what it holds.
    /// </summary>
    [AllowAsSubContext]
    public class FakeAdsContext : Context
    {
        private IAdsService _ads;
        private FakeAdsProvider _provider;

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            _provider = InjectionBinderCrossContext.Bind<FakeAdsProvider>();
        }

        public override void Setup()
        {
            base.Setup();

            _ads = InjectionBinderCrossContext.GetInstance<IAdsService>();

            if (_ads == null)
            {
                FlowLogger.LogError("Setup - IAdsService is not bound: list this context on AdsServiceRoot.");
                return;
            }

            _ads.Plug(_provider);
        }

        public override void DestroyContext()
        {
            if (_provider != null)
                _ads?.Unplug(_provider);

            base.DestroyContext();
        }
    }
}

#endif
