using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.AssetDeliveryModule.Data.ValueObjects;

namespace Modules.AssetDeliveryModule.Gateway
{
    /// <summary>
    /// The platform's door to its asset packs - Play on Android, Background Assets on iOS, a
    /// stand-in in the Editor. Names are the store's pack names as the manifest records them.
    /// </summary>
    public interface IAssetPackGateway
    {
        bool IsSupported { get; }
        string UnsupportedReason { get; }

        Task<IReadOnlyList<AssetPackStateRVO>> GetStatesAsync(IReadOnlyList<string> packs);

        /// <summary>Fetches the packs named, reporting bytes across the set. True when every one landed.</summary>
        Task<bool> DownloadAsync(IReadOnlyList<string> packs, IProgress<AssetPackProgressRVO> progress);

        Task<bool> RemoveAsync(string pack);
    }
}
