using FlowIoC.AssetModule.Commands;
using FlowIoC.AssetModule.Gateway;
using FlowIoC.AssetModule.Model;
using FlowIoC.AssetModule.Service;
using FlowIoC.AssetModule.Service.Sub;
using FlowIoC.AssetModule.Signals;
using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.AssetModule.RootsContexts
{
    internal class AssetServiceContext : Context
    {
        private AssetSignals _signals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<AssetSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            InjectionBinderCrossContext.Bind<IAssetService, AssetService>();

            // The one door to Addressables; every sub service below goes through it.
            InjectionBinder.Bind<IAddressablesGateway, AddressablesGateway>();

            InjectionBinder.Bind<IAssetRegistryModel, AssetRegistryModel>();
            InjectionBinder.Bind<AssetLoadSubService>();
            InjectionBinder.Bind<AssetGroupSubService>();
            InjectionBinder.Bind<AssetReleaseSubService>();
            InjectionBinder.Bind<AssetPrioritySubService>();
            InjectionBinder.Bind<AssetDownloadSubService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.Incoming.LoadGroupByLabel).ToSequence<LoadGroupCommand>();
            CommandBinder.Bind(_signals.Incoming.ReleaseGroup).ToSequence<ReleaseGroupCommand>();
            CommandBinder.Bind(_signals.Incoming.ReleaseAsset).ToSequence<ReleaseAssetCommand>();
        }
    }
}
