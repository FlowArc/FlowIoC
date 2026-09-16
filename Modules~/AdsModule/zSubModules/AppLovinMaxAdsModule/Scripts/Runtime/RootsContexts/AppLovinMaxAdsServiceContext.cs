using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.AdsModule.AppLovinMaxAdsModule.Data.UnityObjects;
using Modules.AdsModule.AppLovinMaxAdsModule.Services;
using Modules.AdsModule.Services;

namespace Modules.AdsModule.AppLovinMaxAdsModule.RootsContexts
{
    /// <summary>
    /// The MAX plug. Listed on AdsServiceRoot's Sub Context Types; its Setup - the one phase that
    /// may reach across modules - reads the ad units the game filed as shared, gets the service
    /// and plugs the provider. The Root beside it is for a game that prefers the plug on a Root
    /// of its own.
    /// </summary>
    [AllowAsSubContext]
    public class AppLovinMaxAdsServiceContext : Context
    {
        private IAdsService _ads;
        private AppLovinMaxAdsProvider _provider;

        public override void Setup()
        {
            base.Setup();

            _ads = InjectionBinderCrossContext.GetInstance<IAdsService>();

            if (_ads == null)
            {
                FlowLogger.LogError("Setup - IAdsService is not bound: list this context on AdsServiceRoot, or put that Root in the scene.");
                return;
            }

            // A missing filing is reported by the shared data model itself, with the one fix;
            // the provider then answers that it cannot initialize.
            CD_AppLovinMaxAds units = InjectionBinderCrossContext.GetInstance<ISharedDataModel>().GetScriptable<CD_AppLovinMaxAds>();

            _provider = new AppLovinMaxAdsProvider(units);
            _ads.Plug(_provider);
        }

        public override void DestroyContext()
        {
            // The service reference is kept from Setup: at teardown the host's own context is
            // destroyed before its sub-contexts, so its cross-context bindings may already be gone.
            if (_provider != null)
                _ads?.Unplug(_provider);

            base.DestroyContext();
        }
    }
}
