using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.AssetDeliveryModule.Data.ValueObjects;

namespace Modules.AssetDeliveryModule.Services
{
    /// <summary>
    /// Store-delivered content: what the build declared, what is on the device, and fetching the
    /// rest. Loads go through IAssetService as always; this only makes sure the bundles a load
    /// will ask for are present. Pack names are the store's, as the build's manifest wrote them.
    /// </summary>
    public partial interface IAssetDeliveryService
    {
        Task<IReadOnlyList<AssetPackRVO>> GetPacksAsync();

        /// <summary>Bytes still to fetch for the packs named; 0 when they are all on the device.</summary>
        Task<long> GetPendingSizeAsync(IReadOnlyList<string> packs);

        /// <summary>Fetches what is missing among the packs named. True when every one is on the device afterwards.</summary>
        Task<bool> EnsureAsync(IReadOnlyList<string> packs, IProgress<AssetPackProgressRVO> progress = null);

        Task<bool> RemoveAsync(string pack);

        /// <summary>
        /// The steps a game binds. EnsurePromised is the boot's: every pack the store promised
        /// before the first open, drawn on the Content step. Ensure is a flow's: one pack, held
        /// until it is there. Each step is a file of its own, <c>IAssetDeliveryService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}
