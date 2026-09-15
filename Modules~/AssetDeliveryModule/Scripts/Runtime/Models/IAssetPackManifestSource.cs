using System.Threading.Tasks;

namespace Modules.AssetDeliveryModule.Models
{
    /// <summary>The manifest's text, or null when the build wrote none.</summary>
    public interface IAssetPackManifestSource
    {
        Task<string> ReadAsync();
    }
}
