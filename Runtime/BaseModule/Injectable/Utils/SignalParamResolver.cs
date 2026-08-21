using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.Injectable.Utils
{
    /// <summary>
    /// Fills the <c>[SignalParam]</c> properties of a command from a dispatched signal
    /// payload. Explicitly indexed properties are assigned first so that an unindexed
    /// property's result never depends on where the indexed ones sit in the file.
    /// </summary>
    internal sealed class SignalParamResolver
    {
        private readonly SignalParamCandidateFinder _candidateFinder = new SignalParamCandidateFinder();
        private readonly Dictionary<Type, List<int>> _candidatesByType = new Dictionary<Type, List<int>>();
        private readonly List<SignalParamDiagnostic> _diagnostics = new List<SignalParamDiagnostic>();
        private string[] _claimedBy = Array.Empty<string>();

        public IReadOnlyList<SignalParamDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// True while <see cref="Resolve"/> is running. A [SignalParam] setter that
        /// dispatches re-enters injection on the same context, which would otherwise
        /// let the nested call clear the buffers of the one still in progress.
        /// </summary>
        public bool IsResolving { get; private set; }

        public void Resolve(object target, IReadOnlyList<SignalParamEntry> entries, object[] values)
        {
            IsResolving = true;
            try
            {
                ResolveCore(target, entries, values);
            }
            finally
            {
                IsResolving = false;
            }
        }

        private void ResolveCore(object target, IReadOnlyList<SignalParamEntry> entries, object[] values)
        {
            _diagnostics.Clear();
            _candidatesByType.Clear();

            if (target == null || entries == null || entries.Count == 0)
                return;

            if (values == null || values.Length == 0)
                return;

            if (_claimedBy.Length < values.Length)
                _claimedBy = new string[values.Length];
            else
                Array.Clear(_claimedBy, 0, values.Length);

            Type targetType = target.GetType();

            ResolveIndexed(target, targetType, entries, values);
            ResolveUnindexed(target, targetType, entries, values);
        }

        private void ResolveIndexed(object target, Type targetType, IReadOnlyList<SignalParamEntry> entries, object[] values)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                SignalParamEntry entry = entries[i];
                if (!entry.HasIndex)
                    continue;

                List<int> candidates = GetCandidates(entry.Type, values);

                if (entry.Index < 0 || entry.Index >= candidates.Count)
                {
                    Report(SignalParamDiagnosticKind.IndexOutOfRange, targetType, entry, candidates.Count, 0, null);
                    continue;
                }

                int slot = candidates[entry.Index];
                if (_claimedBy[slot] != null)
                {
                    Report(SignalParamDiagnosticKind.DuplicateClaim, targetType, entry, candidates.Count, 0, _claimedBy[slot]);
                    continue;
                }

                _claimedBy[slot] = entry.Property.Name;
                entry.Property.SetValue(target, values[slot]);
            }
        }

        private void ResolveUnindexed(object target, Type targetType, IReadOnlyList<SignalParamEntry> entries, object[] values)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                SignalParamEntry entry = entries[i];
                if (entry.HasIndex)
                    continue;

                List<int> candidates = GetCandidates(entry.Type, values);

                int slot = -1;
                int nullSlot = -1;
                int claimedCount = 0;

                for (int c = 0; c < candidates.Count; c++)
                {
                    int candidate = candidates[c];

                    if (_claimedBy[candidate] != null)
                    {
                        claimedCount++;
                        continue;
                    }

                    if (values[candidate] != null)
                    {
                        slot = candidate;
                        break;
                    }

                    // Remember the first free null but keep looking — a real value further
                    // along the payload is a better match than a null before it.
                    if (nullSlot < 0)
                        nullSlot = candidate;
                }

                if (slot < 0)
                    slot = nullSlot;

                if (slot < 0)
                {
                    Report(SignalParamDiagnosticKind.NoFreeSlot, targetType, entry, candidates.Count, claimedCount, null);
                    continue;
                }

                _claimedBy[slot] = entry.Property.Name;
                entry.Property.SetValue(target, values[slot]);
            }
        }

        private List<int> GetCandidates(Type type, object[] values)
        {
            if (_candidatesByType.TryGetValue(type, out List<int> cached))
                return cached;

            List<int> candidates = _candidateFinder.Find(type, values);
            _candidatesByType[type] = candidates;
            return candidates;
        }

        private void Report(SignalParamDiagnosticKind kind, Type targetType, SignalParamEntry entry, int candidateCount, int claimedCount, string claimingPropertyName)
        {
            _diagnostics.Add(new SignalParamDiagnostic(
                kind, targetType, entry.Property.Name, entry.Type,
                entry.Index, candidateCount, claimedCount, claimingPropertyName));
        }
    }
}
