using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;
using FlowIoC.AssetModule.Extensions;
using FlowIoC.AssetModule.Gateway;
using FlowIoC.AssetModule.Signals;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace FlowIoC.AssetModule.Service.Sub
{
    /// <summary>
    /// Fetching bundles ahead of a load, for a remote catalogue. A download is not a claim:
    /// nothing here touches the registry, and the bundles it brought stay in Addressables' cache
    /// for the loads that follow.
    /// </summary>
    internal sealed class AssetDownloadSubService
    {
        [Inject] private IAddressablesGateway _gateway { get; set; }
        [Inject] private AssetPrioritySubService _priority { get; set; }
        [InjectSignal] private AssetSignals _signals { get; set; }

        public Task<long> GetDownloadSizeAsync(object keyOrLabel)
        {
            if (AssetKeyExtensions.TryNormalize(keyOrLabel, out _, out var runtimeKey))
                return _gateway.GetDownloadSizeAsync(runtimeKey);

            FlowLogger.LogError(SystemLogType.Asset, "[AssetService] GetDownloadSizeAsync: invalid key.");
            return Task.FromResult(0L);
        }

        public async Task<bool> DownloadDependenciesAsync(object keyOrLabel, AssetLoadOptions options = default)
        {
            if (!AssetKeyExtensions.TryNormalize(keyOrLabel, out var regKey, out var runtimeKey))
            {
                FlowLogger.LogError(SystemLogType.Asset, "[AssetService] DownloadDependenciesAsync: invalid key.");
                return false;
            }

            if (options.Background) _priority.Enter();

            IAssetHandle handle = _gateway.DownloadDependencies(runtimeKey);

            try
            {
                while (!handle.IsDone)
                {
                    options.Progress?.Report(handle.PercentComplete);
                    await Task.Yield();
                }

                options.Progress?.Report(1f);

                if (handle.Succeeded)
                {
                    FlowLogger.Log(SystemLogType.Asset, $"[AssetDownloadSubService.DownloadDependenciesAsync][key({regKey})]");
                    return true;
                }

                FlowLogger.LogError(SystemLogType.Asset, $"[AssetService] Download failed: {regKey}");
                _signals.Outgoing.AssetLoadFailed.Dispatch(regKey);
                return false;
            }
            finally
            {
                handle.Release();
                if (options.Background) _priority.Exit();
            }
        }
    }
}
