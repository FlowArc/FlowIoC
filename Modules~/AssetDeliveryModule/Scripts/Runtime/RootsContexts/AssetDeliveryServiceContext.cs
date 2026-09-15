using FlowIoC.BaseModule.Contexts;
using Modules.AssetDeliveryModule.Gateway;
using Modules.AssetDeliveryModule.Models;
using Modules.AssetDeliveryModule.Services;

namespace Modules.AssetDeliveryModule.RootsContexts
{
    public class AssetDeliveryServiceContext : Context
    {
        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinderCrossContext.Bind<IAssetDeliveryService, AssetDeliveryService>();

            InjectionBinder.Bind<IAssetPackManifestSource, AddressablesRuntimeManifestSource>();
            InjectionBinder.Bind<IAssetPackManifestModel, AssetPackManifestModel>();

            // The platform's door. The Editor has no store, so it answers that everything is here.
#if UNITY_EDITOR
            InjectionBinder.Bind<IAssetPackGateway, EditorAssetPackGateway>();
#elif UNITY_ANDROID
            InjectionBinder.Bind<IAssetPackGateway, AndroidAssetPackGateway>();
#elif UNITY_IOS
            InjectionBinder.Bind<IAssetPackGateway, IosAssetPackGateway>();
#else
            InjectionBinder.Bind<IAssetPackGateway, EditorAssetPackGateway>();
#endif
        }
    }
}