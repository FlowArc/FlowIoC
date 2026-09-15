using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.AssetDeliveryModule.Data.ValueObjects;

namespace Modules.AssetDeliveryModule.Models
{
    public interface IAssetPackManifestModel
    {
        Task<IReadOnlyList<AssetPackVO>> GetPacksAsync();
        Task<AssetPackVO> FindByBundleAsync(string bundle);
    }
}
