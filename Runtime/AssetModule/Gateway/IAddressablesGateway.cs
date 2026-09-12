using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace FlowIoC.AssetModule.Gateway
{
    /// <summary>
    /// Every call the module makes into Addressables, behind one door. The registry, the
    /// ownership and the in-flight sharing are the module's own logic and are tested through a
    /// fake of this; AddressablesGateway is the one file that names UnityEngine.AddressableAssets.
    /// </summary>
    internal interface IAddressablesGateway
    {
        IAssetHandle LoadAsset<T>(object runtimeKey);

        /// <summary>The primary keys of every location a label resolves to for a type.</summary>
        Task<IReadOnlyList<string>> LoadResourceLocationsAsync(string label, Type type);

        Task<long> GetDownloadSizeAsync(object keyOrLabel);

        /// <summary>The handle is the caller's to release once it has read the outcome.</summary>
        IAssetHandle DownloadDependencies(object keyOrLabel);

        ThreadPriority BackgroundLoadingPriority { get; set; }
    }
}
