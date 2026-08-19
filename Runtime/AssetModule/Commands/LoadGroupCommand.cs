using FlowIoC.AssetModule.Service;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace FlowIoC.AssetModule.Commands
{
    internal sealed class LoadGroupCommand : Command
    {
        [Inject] private IAssetService _assetService { get; set; }
        [SignalParam] private string _label { get; set; }

        public override async void Execute()
        {
            Retain();

            FlowLogger.Log(SystemLogType.Asset,
                $"{nameof(Execute)} - {nameof(LoadGroupCommand)} | label={_label}");

            await _assetService.LoadGroupByLabelAsync<UnityEngine.Object>(_label);

            Release();
        }
    }
}
