using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace FlowIoC.AssetModule.Gateway
{
    internal sealed class AddressablesGateway : IAddressablesGateway
    {
        public IAssetHandle LoadAsset<T>(object runtimeKey) =>
            new AddressableHandle(Addressables.LoadAssetAsync<T>(runtimeKey));

        public async Task<IReadOnlyList<string>> LoadResourceLocationsAsync(string label, Type type)
        {
            var handle = Addressables.LoadResourceLocationsAsync(label, type);
            await handle.Task;

            var keys = new List<string>();

            if (handle.IsValid() && handle.Result != null)
            {
                foreach (IResourceLocation location in handle.Result)
                    keys.Add(location.PrimaryKey);
            }

            if (handle.IsValid())
                Addressables.Release(handle);

            return keys;
        }

        public async Task<long> GetDownloadSizeAsync(object keyOrLabel)
        {
            var handle = Addressables.GetDownloadSizeAsync(keyOrLabel);
            await handle.Task;

            long size = handle.IsValid() ? handle.Result : 0L;

            if (handle.IsValid())
                Addressables.Release(handle);

            return size;
        }

        public IAssetHandle DownloadDependencies(object keyOrLabel) =>
            new AddressableHandle(Addressables.DownloadDependenciesAsync(keyOrLabel));

        public ThreadPriority BackgroundLoadingPriority
        {
            get => Application.backgroundLoadingPriority;
            set => Application.backgroundLoadingPriority = value;
        }
    }
}
