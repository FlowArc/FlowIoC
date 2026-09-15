using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Data.ValueObjects;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Modules.AssetDeliveryModule.Gateway
{
    /// <summary>
    /// Play Asset Delivery through Unity's AndroidAssetPacks. A pack whose path Play knows is on
    /// the device; the rest are asked for in one request, and Play's callbacks carry the bytes.
    /// A phone on mobile data is asked once, through Play's own dialog, before the download goes
    /// on. Every AndroidAssetPacks call throws InvalidOperationException when the Play Core
    /// library is not in the build - an APK installed by hand rather than an AAB - and that is
    /// reported once as the reason nothing came.
    /// </summary>
    public class AndroidAssetPackGateway : IAssetPackGateway
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        public bool IsSupported => true;
        public string UnsupportedReason => string.Empty;

        public Task<IReadOnlyList<AssetPackStateRVO>> GetStatesAsync(IReadOnlyList<string> packs)
        {
            var completion = new TaskCompletionSource<IReadOnlyList<AssetPackStateRVO>>();
            string[] names = new List<string>(packs).ToArray();

            try
            {
                AndroidAssetPacks.GetAssetPackStateAsync(names, (totalBytes, states) =>
                {
                    var result = new List<AssetPackStateRVO>(states.Length);

                    foreach (AndroidAssetPackState state in states)
                    {
                        bool onDevice = state.status == AndroidAssetPackStatus.Completed
                                        && !string.IsNullOrEmpty(AndroidAssetPacks.GetAssetPackPath(state.name));
                        result.Add(new AssetPackStateRVO { Pack = state.name, OnDevice = onDevice });
                    }

                    // Play reports one total for the whole request; per-pack sizes come with the download.
                    if (result.Count == 1) result[0].TotalBytes = (long) totalBytes;

                    completion.TrySetResult(result);
                });
            }
            catch (InvalidOperationException exception)
            {
                FlowLogger.LogError($"AndroidAssetPackGateway.GetStatesAsync - Play Core is not in this build, so no pack can be asked about: {exception.Message}");
                var fallback = new List<AssetPackStateRVO>();
                foreach (string pack in packs) fallback.Add(new AssetPackStateRVO { Pack = pack, OnDevice = false });
                completion.TrySetResult(fallback);
            }

            return completion.Task;
        }

        public Task<bool> DownloadAsync(IReadOnlyList<string> packs, IProgress<AssetPackProgressRVO> progress)
        {
            var completion = new TaskCompletionSource<bool>();
            string[] names = new List<string>(packs).ToArray();
            var pending = new HashSet<string>(names);
            var sizes = new Dictionary<string, (long total, long downloaded)>();
            bool askedForMobileData = false;

            void Report()
            {
                var snapshot = new AssetPackProgressRVO();

                foreach (KeyValuePair<string, (long total, long downloaded)> pair in sizes)
                {
                    snapshot.TotalBytes += pair.Value.total;
                    snapshot.DownloadedBytes += pair.Value.downloaded;
                }

                progress?.Report(snapshot);
            }

            void OnInfo(AndroidAssetPackInfo info)
            {
                sizes[info.name] = ((long) info.size, (long) info.bytesDownloaded);

                switch (info.status)
                {
                    case AndroidAssetPackStatus.Pending:
                    case AndroidAssetPackStatus.Downloading:
                    case AndroidAssetPackStatus.Transferring:
                        Report();
                        break;

                    case AndroidAssetPackStatus.WaitingForWifi:
                        if (askedForMobileData) break;
                        askedForMobileData = true;
                        AndroidAssetPacks.RequestToUseMobileDataAsync(answer =>
                        {
                            if (answer.allowed) return;
                            FlowLogger.LogError("AndroidAssetPackGateway.DownloadAsync - the player did not allow mobile data, so the packs wait for Wi-Fi.");
                            completion.TrySetResult(false);
                        });
                        break;

                    case AndroidAssetPackStatus.Completed:
                        sizes[info.name] = ((long) info.size, (long) info.size);
                        Report();
                        pending.Remove(info.name);
                        if (pending.Count == 0) completion.TrySetResult(true);
                        break;

                    case AndroidAssetPackStatus.Failed:
                    case AndroidAssetPackStatus.Canceled:
                    case AndroidAssetPackStatus.Unknown:
                    case AndroidAssetPackStatus.NotInstalled:
                        FlowLogger.LogError($"AndroidAssetPackGateway.DownloadAsync - '{info.name}' {info.status}: {info.error}");
                        completion.TrySetResult(false);
                        break;
                }
            }

            try
            {
                AndroidAssetPacks.DownloadAssetPackAsync(names, OnInfo);
            }
            catch (InvalidOperationException exception)
            {
                FlowLogger.LogError($"AndroidAssetPackGateway.DownloadAsync - Play Core is not in this build, so nothing can download: {exception.Message}");
                completion.TrySetResult(false);
            }

            return completion.Task;
        }

        public Task<bool> RemoveAsync(string pack)
        {
            try
            {
                AndroidAssetPacks.RemoveAssetPack(pack);
                return Task.FromResult(true);
            }
            catch (InvalidOperationException exception)
            {
                FlowLogger.LogError($"AndroidAssetPackGateway.RemoveAsync - '{pack}' could not be removed: {exception.Message}");
                return Task.FromResult(false);
            }
        }
#else
        public bool IsSupported => false;
        public string UnsupportedReason => "Play Asset Delivery is only available in an Android player";

        public Task<IReadOnlyList<AssetPackStateRVO>> GetStatesAsync(IReadOnlyList<string> packs) =>
            Task.FromResult<IReadOnlyList<AssetPackStateRVO>>(new List<AssetPackStateRVO>());

        public Task<bool> DownloadAsync(IReadOnlyList<string> packs, IProgress<AssetPackProgressRVO> progress) => Task.FromResult(false);

        public Task<bool> RemoveAsync(string pack) => Task.FromResult(false);
#endif
    }
}
