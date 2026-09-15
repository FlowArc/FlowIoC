using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AOT;
using FlowIoC.ConsoleModule;
using Modules.AssetDeliveryModule.Data.ValueObjects;

namespace Modules.AssetDeliveryModule.Gateway
{
    /// <summary>
    /// Apple-hosted managed asset packs through FlowAssetPacks.mm and BAAssetPackManager. iOS 26
    /// is where managed packs begin; below it the gateway says so and the boot's step fails with
    /// that reason. Written against Apple's documentation and unverified on a device; the day
    /// one is available, the selectors in FlowAssetPacks.mm are the first thing to check.
    ///
    /// The request maps are static because a native callback cannot reach an instance - the
    /// one shape MonoPInvokeCallback allows. The plugin calls back on the main thread.
    /// </summary>
    public class IosAssetPackGateway : IAssetPackGateway
    {
        private static readonly Dictionary<int, TaskCompletionSource<AssetPackStateRVO>> _stateRequests = new();

        private static readonly Dictionary<int, (TaskCompletionSource<bool> done, IProgress<AssetPackProgressRVO> progress)>
            _downloadRequests = new();

        private static readonly Dictionary<int, TaskCompletionSource<bool>> _removeRequests = new();
        private static int _nextRequest;

        public bool IsSupported => FlowAssetPacksBridge.IsSupported();
        public string UnsupportedReason => IsSupported ? string.Empty : "managed asset packs need iOS 26 or later";

        public async Task<IReadOnlyList<AssetPackStateRVO>> GetStatesAsync(IReadOnlyList<string> packs)
        {
            var states = new List<AssetPackStateRVO>(packs.Count);

            foreach (string pack in packs)
            {
                var completion = new TaskCompletionSource<AssetPackStateRVO>();
                int id = ++_nextRequest;
                _stateRequests[id] = completion;
                FlowAssetPacksBridge.GetStatus(pack, id, OnStatus);
                states.Add(await completion.Task);
            }

            return states;
        }

        public Task<bool> DownloadAsync(IReadOnlyList<string> packs, IProgress<AssetPackProgressRVO> progress)
        {
            var completion = new TaskCompletionSource<bool>();
            int id = ++_nextRequest;
            _downloadRequests[id] = (completion, progress);
            FlowAssetPacksBridge.Ensure(string.Join(",", packs), id, OnProgress, OnDownloadDone);
            return completion.Task;
        }

        public Task<bool> RemoveAsync(string pack)
        {
            var completion = new TaskCompletionSource<bool>();
            int id = ++_nextRequest;
            _removeRequests[id] = completion;
            FlowAssetPacksBridge.Remove(pack, id, OnRemoveDone);
            return completion.Task;
        }

        [MonoPInvokeCallback(typeof(FlowAssetPacksBridge.StatusCallback))]
        private static void OnStatus(int requestId, string pack, int status, long totalBytes, long downloadedBytes, string error)
        {
            if (!_stateRequests.Remove(requestId, out TaskCompletionSource<AssetPackStateRVO> completion)) return;

            if (status == FlowAssetPacksBridge.STATUS_ERROR)
                FlowLogger.LogError($"IosAssetPackGateway - the status of '{pack}' could not be read: {error}");

            completion.TrySetResult(new AssetPackStateRVO
            {
                Pack = pack,
                OnDevice = status == FlowAssetPacksBridge.STATUS_ON_DEVICE,
                TotalBytes = totalBytes,
                DownloadedBytes = downloadedBytes
            });
        }

        [MonoPInvokeCallback(typeof(FlowAssetPacksBridge.ProgressCallback))]
        private static void OnProgress(int requestId, long totalBytes, long downloadedBytes)
        {
            if (_downloadRequests.TryGetValue(requestId, out (TaskCompletionSource<bool> done, IProgress<AssetPackProgressRVO> progress) request))
                request.progress?.Report(new AssetPackProgressRVO { TotalBytes = totalBytes, DownloadedBytes = downloadedBytes });
        }

        [MonoPInvokeCallback(typeof(FlowAssetPacksBridge.DoneCallback))]
        private static void OnDownloadDone(int requestId, bool ok, string error)
        {
            if (!_downloadRequests.Remove(requestId, out (TaskCompletionSource<bool> done, IProgress<AssetPackProgressRVO> progress) request)) return;

            if (!ok) FlowLogger.LogError($"IosAssetPackGateway - the packs did not land: {error}");

            request.done.TrySetResult(ok);
        }

        [MonoPInvokeCallback(typeof(FlowAssetPacksBridge.DoneCallback))]
        private static void OnRemoveDone(int requestId, bool ok, string error)
        {
            if (!_removeRequests.Remove(requestId, out TaskCompletionSource<bool> completion)) return;

            if (!ok) FlowLogger.LogError($"IosAssetPackGateway - the pack was not removed: {error}");

            completion.TrySetResult(ok);
        }
    }
}
