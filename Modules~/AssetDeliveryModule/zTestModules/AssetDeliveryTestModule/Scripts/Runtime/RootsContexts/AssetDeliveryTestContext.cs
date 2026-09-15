#if UNITY_EDITOR
using FlowIoC.BaseModule.Connectors;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.Service;
using Modules.AssetDeliveryModule.AssetDeliveryTestModule.Signals;
using Modules.AssetDeliveryModule.Services;
using Modules.LoadingModule.LoadingScreenModule.Signals;
using Modules.LoadingModule.Services;
using Modules.LoadingModule.Shared.Constants;

namespace Modules.AssetDeliveryModule.AssetDeliveryTestModule.RootsContexts
{
    /// <summary>
    /// The shipped boot's shape with the Content step in it: the loading screens into the pool,
    /// the set begun, the promised packs ensured, the set awaited. In the Editor nothing is
    /// missing, so the bar shows Content skipped and the set completes; on a device with a
    /// fast-follow pack not yet landed, the bar shows the download, and a pack that will not
    /// come fails the set and the retry runs the boot again. The config is
    /// CD_LoadingSets_AssetDeliveryTest on this scene's LoadingServiceRoot.
    /// </summary>
    public class AssetDeliveryTestContext : Context
    {
        private const string BOOT_SET = "Boot";

        private AssetDeliveryTestInternalSignals _internalSignals;
        private LoadingScreenSignals _loadingScreenSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _internalSignals = InjectionBinder.Bind<AssetDeliveryTestInternalSignals>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_internalSignals.Launch)
                .ToSequence<IScreenService.Commands.LoadByTag>(LoadingConstants.SCREEN_TAG)
                .ToSequence<ILoadingService.Commands.Begin>(BOOT_SET)
                .ToSequence<IAssetDeliveryService.Commands.EnsurePromised>()
                .ToSequence<ILoadingService.Commands.Await>(BOOT_SET);
        }

        public override void Setup()
        {
            base.Setup();
            _loadingScreenSignals = InjectionBinderCrossContext.GetInstance<LoadingScreenSignals>();
            _loadingScreenSignals.Outgoing.RetryClicked.Connect(_ => _internalSignals.Launch.Dispatch());
        }

        public override void Launch()
        {
            base.Launch();
            _internalSignals.Launch.Dispatch();
        }

        public override void DestroyContext()
        {
            _loadingScreenSignals?.Outgoing.RetryClicked.Disconnect();
            base.DestroyContext();
        }
    }
}
#endif