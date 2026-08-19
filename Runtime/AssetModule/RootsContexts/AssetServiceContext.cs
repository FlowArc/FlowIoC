using FlowIoC.AssetModule.Commands;
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

            InjectionBinder.Bind<IAssetRegistryModel, AssetRegistryModel>();
            InjectionBinder.Bind<AssetLoadSubService>();
            InjectionBinder.Bind<AssetGroupSubService>();
            InjectionBinder.Bind<AssetReleaseSubService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();

            CommandBinder.Bind(_signals.InComing.LoadGroupByLabel).ToSequence<LoadGroupCommand>();
            CommandBinder.Bind(_signals.InComing.ReleaseGroup).ToSequence<ReleaseGroupCommand>();
            CommandBinder.Bind(_signals.InComing.ReleaseAsset).ToSequence<ReleaseAssetCommand>();
        }
    }
}
