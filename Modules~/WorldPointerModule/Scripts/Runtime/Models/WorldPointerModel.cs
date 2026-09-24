using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.WorldPointerModule.Data.UnityObjects;
using Modules.WorldPointerModule.Data.ValueObjects;
using Modules.WorldPointerModule.Enums;
using Modules.WorldPointerModule.RootsContexts;
using Modules.WorldPointerModule.Services;
using Modules.WorldPointerModule.Services.Sub;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modules.WorldPointerModule.Models
{
    /// <summary>
    /// Channels by id and targets by channel and Transform, each found in one step, and each
    /// channel's members in a list a target leaves in one step by swapping with the last. The same
    /// row objects sit in RD_WorldPointer when the Root's adapter has one, so the Inspector shows
    /// them as they are written and there is nothing to publish. The asset is optional and emptied
    /// at boot: a ScriptableObject edited in play mode keeps the edit on disk.
    /// </summary>
    internal class WorldPointerModel : IWorldPointerModel, IConstructable
    {
        [Inject(nameof(WorldPointerServiceContext))]
        private GameObject _root { get; set; }

        private readonly List<WorldPointerChannelRVO> _channels = new();
        private readonly Dictionary<string, WorldPointerChannelRVO> _channelsById = new();
        private readonly Dictionary<TargetKey, WorldPointerTargetRVO> _targets = new();

        private RD_WorldPointer _status;

        public bool IsPostConstructed { get; set; }

        public bool IsDeconstructed { get; set; }

        public IReadOnlyList<WorldPointerChannelRVO> Channels => _channels;

        public int TargetCount => _targets.Count;

        public void PostConstruct()
        {
            RootAdapter adapter = _root != null ? _root.GetComponent<RootAdapter>() : null;

            if (adapter == null)
            {
                FlowLogger.LogError("WorldPointerServiceRoot has no RootAdapter, so RD_WorldPointer cannot be read; nothing is shown in it.", _root);
                return;
            }

            Load(adapter.GetScriptable<RD_WorldPointer>());
        }

        /// <summary>Holds the asset, empties it and puts the channels already held in it. Internal so a test loads one without a Root.</summary>
        internal void Load(RD_WorldPointer status)
        {
            _status = status;
            if (_status == null) return;

            _status.Channels.Clear();
            _status.Channels.AddRange(_channels);
        }

        // ---------------------------------------------------------------- channels

        public WorldPointerChannelRVO GetChannel(string channel) =>
            channel != null && _channelsById.TryGetValue(channel, out WorldPointerChannelRVO found) ? found : null;

        public WorldPointerChannelRVO SetDisplay(string channel, WorldPointerDisplaySlot slot)
        {
            WorldPointerChannelRVO row = GetChannel(channel) ?? AddChannel(channel);

            row.Slot = slot;
            row.Display = slot != null ? slot.Name : string.Empty;

            RemoveIfUnused(row);
            return row;
        }

        private WorldPointerChannelRVO AddChannel(string channel)
        {
            var row = new WorldPointerChannelRVO {Id = channel, Display = string.Empty, Index = _channels.Count};

            _channels.Add(row);
            _channelsById.Add(channel, row);
            _status?.Channels.Add(row);

            return row;
        }

        /// <summary>A channel with no display and no targets says nothing, so it goes.</summary>
        private void RemoveIfUnused(WorldPointerChannelRVO row)
        {
            if (row.Slot != null || row.Members.Count > 0) return;

            SwapRemove(_channels, row.Index, (moved, index) => moved.Index = index);
            _channelsById.Remove(row.Id);
            _status?.Channels.Remove(row);
        }

        // ---------------------------------------------------------------- targets

        public bool TryGetTarget(string channel, Transform target, out WorldPointerTargetRVO entry) =>
            _targets.TryGetValue(new TargetKey(channel, target), out entry);

        public WorldPointerTargetRVO AddTarget(string channel, Transform target)
        {
            WorldPointerChannelRVO row = GetChannel(channel) ?? AddChannel(channel);

            var entry = new WorldPointerTargetRVO
            {
                Target = target,
                Owner = row,
                Index = row.Members.Count,
                ContentText = string.Empty
            };

            row.Members.Add(entry);
            row.Targets.Add(entry);
            _targets.Add(new TargetKey(channel, target), entry);

            return entry;
        }

        public void RemoveTarget(WorldPointerTargetRVO entry)
        {
            WorldPointerChannelRVO row = entry.Owner;
            if (!_targets.Remove(new TargetKey(row.Id, entry.Target))) return;

            RemoveRow(row.Targets, entry, entry.Index);
            SwapRemove(row.Members, entry.Index, (moved, index) => moved.Index = index);

            RemoveIfUnused(row);
        }

        public void ClearTargets()
        {
            _targets.Clear();

            for (int i = _channels.Count - 1; i >= 0; i--)
            {
                WorldPointerChannelRVO row = _channels[i];
                row.Members.Clear();
                row.Targets.Clear();
                RemoveIfUnused(row);
            }
        }

        // ---------------------------------------------------------------- one target's rows

        public void SetContent(WorldPointerTargetRVO entry, object content)
        {
            entry.Value = content;
            entry.HasContent = true;
            entry.ContentText = content?.ToString() ?? "null";

            // SerializeReference takes a plain class and nothing else.
            entry.Content = content != null && content.GetType().IsClass && content is not string && content is not Object
                ? content
                : null;
        }

        public void SetVisible(WorldPointerTargetRVO entry, bool visible) => entry.Visible = visible;

        public void SetIndicator(WorldPointerTargetRVO entry, IWorldPointerIndicator indicator, Camera canvasCamera)
        {
            entry.Indicator = indicator;
            entry.CanvasCamera = canvasCamera;
            entry.Shown = indicator != null;
            entry.HasState = false;
            entry.State = WorldPointerState.Hidden;
        }

        public bool SetState(WorldPointerTargetRVO entry, WorldPointerState state)
        {
            if (entry.HasState && entry.State == state) return false;

            entry.HasState = true;
            entry.State = state;
            return true;
        }

        // ---------------------------------------------------------------- lists

        /// <summary>Moves the last item into the gap and tells it its new index - no shifting.</summary>
        private static void SwapRemove<T>(List<T> list, int index, Action<T, int> reindex)
        {
            int last = list.Count - 1;

            if (index < last)
            {
                list[index] = list[last];
                reindex(list[index], index);
            }

            list.RemoveAt(last);
        }

        /// <summary>
        /// The Inspector's copy of the member list, kept in the same order by the same swap. A row
        /// list replaced by an Inspector edit no longer matches, and then the row is found by search.
        /// </summary>
        private static void RemoveRow(List<WorldPointerTargetRVO> rows, WorldPointerTargetRVO entry, int index)
        {
            if (index < rows.Count && ReferenceEquals(rows[index], entry))
                SwapRemove(rows, index, (_, _) => { });
            else
                rows.Remove(entry);
        }

        /// <summary>
        /// A channel and a Transform. The Transform is compared by reference and hashed by identity,
        /// so a destroyed one still finds its entry and no Unity equality runs.
        /// </summary>
        private readonly struct TargetKey : IEquatable<TargetKey>
        {
            private readonly string _channel;
            private readonly Transform _target;

            public TargetKey(string channel, Transform target)
            {
                _channel = channel;
                _target = target;
            }

            public bool Equals(TargetKey other) =>
                ReferenceEquals(_target, other._target) && string.Equals(_channel, other._channel, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is TargetKey other && Equals(other);

            public override int GetHashCode() =>
                HashCode.Combine(_channel != null ? StringComparer.Ordinal.GetHashCode(_channel) : 0,
                    RuntimeHelpers.GetHashCode(_target));
        }
    }
}