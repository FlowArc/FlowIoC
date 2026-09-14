using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;
using FlowIoC.AssetModule.Extensions;
using FlowIoC.AssetModule.Service;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What the screen and pool loaders see of the asset service: a prefab per key, and a record
    /// of every claim and release with the owner it was made under.
    /// </summary>
    internal sealed class FakeAssetService : IAssetService
    {
        public readonly Dictionary<string, GameObject> Prefabs = new();
        public readonly List<(string key, string owner)> Claims = new();
        public readonly List<(string key, string owner)> Releases = new();

        /// <summary>
        /// With this on, a load stays out until the test calls <see cref="Complete"/> for its key -
        /// the window in which a second caller reaches the same entry, which is what the in-flight
        /// tests are about. Off, a load answers at once.
        /// </summary>
        public bool Hold;

        private readonly Dictionary<string, TaskCompletionSource<GameObject>> _held = new();

        private static string KeyOf(object key) => AssetKeyExtensions.TryNormalize(key, out string regKey, out _) ? regKey : null;

        public Task<T> LoadAssetAsync<T>(object key, string groupId = null)
        {
            string regKey = KeyOf(key);
            Claims.Add((regKey, groupId));

            if (Hold && typeof(T) == typeof(GameObject))
            {
                if (!_held.TryGetValue(regKey, out TaskCompletionSource<GameObject> pending))
                {
                    pending = new TaskCompletionSource<GameObject>();
                    _held[regKey] = pending;
                }

                return (Task<T>) (object) pending.Task;
            }

            return Task.FromResult(Prefabs.TryGetValue(regKey, out GameObject prefab) && prefab is T typed ? typed : default);
        }

        /// <summary>Lets a held load land, with the prefab registered under its key.</summary>
        public void Complete(string key)
        {
            if (_held.TryGetValue(key, out TaskCompletionSource<GameObject> pending))
                pending.TrySetResult(Prefabs.TryGetValue(key, out GameObject prefab) ? prefab : null);
        }

        public T LoadAsset<T>(object key, string groupId = null) => LoadAssetAsync<T>(key, groupId).Result;

        public bool TryGetAsset<T>(object key, out T asset)
        {
            asset = Prefabs.TryGetValue(KeyOf(key), out GameObject prefab) && prefab is T typed ? typed : default;
            return asset != null;
        }

        public void Release(object key, string groupId = null) => Releases.Add((KeyOf(key), groupId));

        public Task LoadGroupByLabelAsync<T>(string label, string groupId = null, AssetLoadOptions options = default) => Task.CompletedTask;
        public Task LoadAssetsAsync<T>(string groupId, IEnumerable<object> keys, AssetLoadOptions options = default) => Task.CompletedTask;

        public void AddToGroup(string groupId, object key)
        {
        }

        public void ReleaseGroup(string groupId)
        {
        }

        public bool IsGroupLoaded(string groupId) => false;
        public IReadOnlyCollection<string> GetGroupKeys(string groupId) => System.Array.Empty<string>();
        public Task<long> GetDownloadSizeAsync(object keyOrLabel) => Task.FromResult(0L);
        public Task<bool> DownloadDependenciesAsync(object keyOrLabel, AssetLoadOptions options = default) => Task.FromResult(true);
    }
}