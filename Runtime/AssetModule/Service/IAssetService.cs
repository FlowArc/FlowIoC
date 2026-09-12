using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;

namespace FlowIoC.AssetModule.Service
{
    /// <summary>
    /// Addressable assets, loaded once and shared. A key asked for twice is loaded once: the second
    /// call gets the cached asset, or waits on the load already in flight. Groups load, release and
    /// gather assets in bulk, with progress and, if asked, in the background. Nothing here
    /// instantiates - a caller that needs a prefab instance instantiates what it was handed. This
    /// is the package's one door to Addressables: the screen service and the pool service load
    /// their prefabs through it, each under an owner of its own, so a prefab both touch is loaded
    /// once and released when the last of them lets go.
    /// </summary>
    public interface IAssetService
    {
        Task<T> LoadAssetAsync<T>(object key, string groupId = null);
        T LoadAsset<T>(object key, string groupId = null);
        bool TryGetAsset<T>(object key, out T asset);
        void Release(object key, string groupId = null);

        Task LoadGroupByLabelAsync<T>(string label, string groupId = null, AssetLoadOptions options = default);
        Task LoadAssetsAsync<T>(string groupId, IEnumerable<object> keys, AssetLoadOptions options = default);
        void AddToGroup(string groupId, object key);
        void ReleaseGroup(string groupId);
        bool IsGroupLoaded(string groupId);
        IReadOnlyCollection<string> GetGroupKeys(string groupId);

        /// <summary>Bytes still to fetch for a key or label; 0 when everything is cached.</summary>
        Task<long> GetDownloadSizeAsync(object keyOrLabel);

        /// <summary>Fetches the bundles a key or label needs. Not a claim: nothing is held afterwards.</summary>
        Task<bool> DownloadDependenciesAsync(object keyOrLabel, AssetLoadOptions options = default);
    }
}