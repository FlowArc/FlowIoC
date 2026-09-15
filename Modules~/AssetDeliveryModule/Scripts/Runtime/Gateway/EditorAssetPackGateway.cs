using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.AssetDeliveryModule.Data.ValueObjects;

namespace Modules.AssetDeliveryModule.Gateway
{
    /// <summary>
    /// The Editor and every platform without a store: every pack is on the device already, so a
    /// boot in the Editor skips the Content step and a game's Ensure returns at once.
    /// </summary>
    public class EditorAssetPackGateway : IAssetPackGateway
    {
        public bool IsSupported => true;
        public string UnsupportedReason => string.Empty;

        public Task<IReadOnlyList<AssetPackStateRVO>> GetStatesAsync(IReadOnlyList<string> packs)
        {
            var states = new List<AssetPackStateRVO>(packs.Count);

            foreach (string pack in packs)
                states.Add(new AssetPackStateRVO { Pack = pack, OnDevice = true });

            return Task.FromResult<IReadOnlyList<AssetPackStateRVO>>(states);
        }

        public Task<bool> DownloadAsync(IReadOnlyList<string> packs, IProgress<AssetPackProgressRVO> progress)
        {
            progress?.Report(new AssetPackProgressRVO());
            return Task.FromResult(true);
        }

        public Task<bool> RemoveAsync(string pack) => Task.FromResult(true);
    }
}
