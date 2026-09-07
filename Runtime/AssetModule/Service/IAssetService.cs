using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowIoC.AssetModule.Service
{
    /// <summary>
    /// Addressable assets, loaded once and shared. A key asked for twice is loaded once: the second
    /// call gets the cached asset, or waits on the load already in flight. Groups load, release and
    /// gather assets in bulk. Nothing here instantiates - a caller that needs a prefab instance
    /// instantiates what it was handed.
    /// </summary>
    public interface IAssetService
    {
        Task<T> LoadAssetAsync<T>(object key, string groupId = null);
        T LoadAsset<T>(object key, string groupId = null);
        bool TryGetAsset<T>(object key, out T asset);
        void Release(object key, string groupId = null);

        Task LoadGroupByLabelAsync<T>(string label, string groupId = null);
        Task LoadAssetsAsync<T>(string groupId, IEnumerable<object> keys);
        void AddToGroup(string groupId, object key);
        void ReleaseGroup(string groupId);
        bool IsGroupLoaded(string groupId);
        IReadOnlyCollection<string> GetGroupKeys(string groupId);
    }
}
