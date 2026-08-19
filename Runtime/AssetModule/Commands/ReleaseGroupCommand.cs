using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace FlowIoC.AssetModule.Commands
{
    internal sealed class ReleaseGroupCommand : Command
    {
        [Inject] private IAssetService _assetService { get; set; }
        [SignalParam] private string _groupId { get; set; }

        public override void Execute()
        {
            Retain();

            FlowLogger.Log(SystemLogType.Asset,
                $"{nameof(Execute)} - {nameof(ReleaseGroupCommand)} | groupId={_groupId}");

            _assetService.ReleaseGroup(_groupId);

            Release();
        }
    }
}
