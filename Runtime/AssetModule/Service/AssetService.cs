using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;
using FlowIoC.AssetModule.Service.Sub;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.AssetModule.Service
{
    internal sealed class AssetService : IAssetService
    {
        [Inject] private AssetLoadSubService _load { get; set; }
        [Inject] private AssetGroupSubService _group { get; set; }
        [Inject] private AssetReleaseSubService _release { get; set; }
        [Inject] private AssetDownloadSubService _download { get; set; }

        public Task<T> LoadAssetAsync<T>(object key, string groupId = null) => _load.LoadAssetAsync<T>(key, groupId);
        public T LoadAsset<T>(object key, string groupId = null) => _load.LoadAsset<T>(key, groupId);
        public bool TryGetAsset<T>(object key, out T asset) => _load.TryGetAsset(key, out asset);
        public void Release(object key, string groupId = null) => _release.Release(key, groupId);

        public Task LoadGroupByLabelAsync<T>(string label, string groupId = null, AssetLoadOptions options = default) =>
            _group.LoadGroupByLabelAsync<T>(label, groupId, options);

        public Task LoadAssetsAsync<T>(string groupId, IEnumerable<object> keys, AssetLoadOptions options = default) =>
            _group.LoadAssetsAsync<T>(groupId, keys, options);

        public void AddToGroup(string groupId, object key) => _group.AddToGroup(groupId, key);
        public void ReleaseGroup(string groupId) => _release.ReleaseGroup(groupId);
        public bool IsGroupLoaded(string groupId) => _group.IsGroupLoaded(groupId);
        public IReadOnlyCollection<string> GetGroupKeys(string groupId) => _group.GetGroupKeys(groupId);

        public Task<long> GetDownloadSizeAsync(object keyOrLabel) => _download.GetDownloadSizeAsync(keyOrLabel);

        public Task<bool> DownloadDependenciesAsync(object keyOrLabel, AssetLoadOptions options = default) =>
            _download.DownloadDependenciesAsync(keyOrLabel, options);
    }
}