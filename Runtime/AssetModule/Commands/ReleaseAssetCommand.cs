using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace FlowIoC.AssetModule.Commands
{
    internal sealed class ReleaseAssetCommand : Command
    {
        [Inject] private IAssetService _assetService { get; set; }
        [SignalParam] private string _key { get; set; }

        public override void Execute()
        {
            Retain();

            FlowLogger.Log(SystemLogType.Asset,
                $"{nameof(Execute)} - {nameof(ReleaseAssetCommand)} | key={_key}");

            _assetService.Release(_key);

            Release();
        }
    }
}
