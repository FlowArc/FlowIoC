using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Data.ValueObjects;
using Modules.AssetDeliveryModule.Gateway;
using Modules.AssetDeliveryModule.Models;

namespace Modules.AssetDeliveryModule.Services
{
    public class AssetDeliveryService : IAssetDeliveryService
    {
        [Inject] private IAssetPackManifestModel _manifest { get; set; }
        [Inject] private IAssetPackGateway _gateway { get; set; }

        public async Task<IReadOnlyList<AssetPackRVO>> GetPacksAsync()
        {
            IReadOnlyList<AssetPackVO> packs = await _manifest.GetPacksAsync();
            var result = new List<AssetPackRVO>(packs.Count);

            if (packs.Count == 0) return result;

            var names = new List<string>(packs.Count);
            foreach (AssetPackVO pack in packs) names.Add(pack.Pack);

            IReadOnlyList<AssetPackStateRVO> states = await _gateway.GetStatesAsync(names);

            foreach (AssetPackVO pack in packs)
            {
                AssetPackStateRVO state = FindState(states, pack.Pack);

                result.Add(new AssetPackRVO
                {
                    Pack = pack.Pack,
                    Group = pack.Group,
                    Policy = pack.Policy,
                    OnDevice = state == null || state.OnDevice,
                    TotalBytes = state?.TotalBytes ?? 0
                });
            }

            return result;
        }

        public async Task<long> GetPendingSizeAsync(IReadOnlyList<string> packs)
        {
            IReadOnlyList<AssetPackStateRVO> states = await _gateway.GetStatesAsync(packs);
            long pending = 0;

            foreach (AssetPackStateRVO state in states)
                if (!state.OnDevice)
                    pending += Math.Max(0, state.TotalBytes - state.DownloadedBytes);

            return pending;
        }

        public async Task<bool> EnsureAsync(IReadOnlyList<string> packs, IProgress<AssetPackProgressRVO> progress = null)
        {
            IReadOnlyList<AssetPackVO> known = await _manifest.GetPacksAsync();

            foreach (string pack in packs)
            {
                if (FindPack(known, pack) != null) continue;

                FlowLogger.LogError($"AssetDeliveryService.EnsureAsync - '{pack}' is not a pack this build declared; FlowAssetPacks.json lists {known.Count} pack(s).");
                return false;
            }

            if (!_gateway.IsSupported)
            {
                FlowLogger.LogError($"AssetDeliveryService.EnsureAsync - asset packs are not available here: {_gateway.UnsupportedReason}");
                return false;
            }

            IReadOnlyList<AssetPackStateRVO> states = await _gateway.GetStatesAsync(packs);
            var missing = new List<string>();
            long total = 0;

            foreach (AssetPackStateRVO state in states)
            {
                if (state.OnDevice) continue;
                missing.Add(state.Pack);
                total += state.TotalBytes;
            }

            if (missing.Count == 0)
            {
                progress?.Report(new AssetPackProgressRVO());
                return true;
            }

            FlowLogger.Log($"AssetDeliveryService.EnsureAsync - fetching {missing.Count} pack(s), {total} bytes: {string.Join(", ", missing)}");
            return await _gateway.DownloadAsync(missing, progress);
        }

        public Task<bool> RemoveAsync(string pack) => _gateway.RemoveAsync(pack);

        private AssetPackStateRVO FindState(IReadOnlyList<AssetPackStateRVO> states, string pack)
        {
            foreach (AssetPackStateRVO state in states)
                if (state.Pack == pack)
                    return state;

            return null;
        }

        private AssetPackVO FindPack(IReadOnlyList<AssetPackVO> packs, string pack)
        {
            foreach (AssetPackVO candidate in packs)
                if (candidate.Pack == pack)
                    return candidate;

            return null;
        }
    }
}
