using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.AssetModule.Data;
using FlowIoC.AssetModule.Extensions;
using FlowIoC.AssetModule.Gateway;
using FlowIoC.AssetModule.Model;
using FlowIoC.AssetModule.Signals;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace FlowIoC.AssetModule.Service.Sub
{
    internal sealed class AssetGroupSubService
    {
        [Inject] private IAssetRegistryModel _registry { get; set; }
        [Inject] private AssetLoadSubService _load { get; set; }
        [Inject] private AssetPrioritySubService _priority { get; set; }
        [Inject] private IAddressablesGateway _gateway { get; set; }
        [InjectSignal] private AssetSignals _signals { get; set; }

        public async Task LoadGroupByLabelAsync<T>(string label, string groupId = null, AssetLoadOptions options = default)
        {
            if (string.IsNullOrEmpty(label)) return;

            groupId ??= label;

            IReadOnlyList<string> keys = await _gateway.LoadResourceLocationsAsync(label, typeof(T));

            if (keys == null || keys.Count == 0)
            {
                FlowLogger.LogWarning(SystemLogType.Asset,
                    $"[AssetService] No '{typeof(T).Name}' locations found for label '{label}'. Are the assets labelled?");
                options.Progress?.Report(1f);
                _registry.GetOrCreateGroup(groupId).IsLoaded = true;
                _signals.Outgoing.GroupLoaded.Dispatch(groupId);
                return;
            }

            await LoadKeysAsync<T>(groupId, keys, options);

            FlowLogger.Log(SystemLogType.Asset,
                $"[AssetGroupSubService.LoadGroupByLabelAsync][label({label})][groupId({groupId})][assets({keys.Count})]");
        }

        public async Task LoadAssetsAsync<T>(string groupId, IEnumerable<object> keys, AssetLoadOptions options = default)
        {
            if (string.IsNullOrEmpty(groupId) || keys == null) return;

            await LoadKeysAsync<T>(groupId, keys, options);
        }

        public void AddToGroup(string groupId, object key)
        {
            if (string.IsNullOrEmpty(groupId)) return;
            if (!AssetKeyExtensions.TryNormalize(key, out var regKey, out _)) return;
            if (!_registry.Entries.TryGetValue(regKey, out var entry)) return;

            if (entry.Owners.Add(groupId))
                _registry.GetOrCreateGroup(groupId).Keys.Add(regKey);
        }

        public bool IsGroupLoaded(string groupId)
            => !string.IsNullOrEmpty(groupId)
               && _registry.Groups.TryGetValue(groupId, out var group)
               && group.IsLoaded;

        public IReadOnlyCollection<string> GetGroupKeys(string groupId)
            => _registry.Groups.TryGetValue(groupId, out var group)
                ? group.Keys
                : Array.Empty<string>();

        /// <summary>
        /// One load per key, all in flight at once; the group is marked loaded and announced when
        /// the last of them lands. Progress is the mean of the handles' own percentages, sampled
        /// once per frame, and a background load holds the priority down for as long as any of
        /// its keys is still loading.
        /// </summary>
        private async Task LoadKeysAsync<T>(string groupId, IEnumerable<object> keys, AssetLoadOptions options)
        {
            var tasks = new List<Task>();
            var regKeys = new List<string>();

            foreach (var key in keys)
            {
                if (AssetKeyExtensions.TryNormalize(key, out var regKey, out _))
                    regKeys.Add(regKey);

                tasks.Add(_load.LoadAssetAsync<T>(key, groupId));
            }

            if (options.Background) _priority.Enter();

            try
            {
                if (options.Progress != null)
                    await ReportUntilDone(tasks, regKeys, options.Progress);
                else
                    await Task.WhenAll(tasks);
            }
            finally
            {
                if (options.Background) _priority.Exit();
            }

            _registry.GetOrCreateGroup(groupId).IsLoaded = true;
            _signals.Outgoing.GroupLoaded.Dispatch(groupId);
        }

        private async Task ReportUntilDone(List<Task> tasks, List<string> regKeys, IProgress<float> progress)
        {
            Task all = Task.WhenAll(tasks);

            while (!all.IsCompleted)
            {
                progress.Report(Mean(regKeys));
                await Task.Yield();
            }

            progress.Report(1f);
        }

        private float Mean(List<string> regKeys)
        {
            if (regKeys.Count == 0) return 1f;

            float sum = 0f;

            foreach (string regKey in regKeys)
            {
                bool loaded = _registry.Entries.TryGetValue(regKey, out var entry);
                sum += loaded && entry.Handle != null ? entry.Handle.PercentComplete : loaded && entry.Result != null ? 1f : 0f;
            }

            return sum / regKeys.Count;
        }
    }
}