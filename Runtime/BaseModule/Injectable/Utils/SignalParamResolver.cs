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

        public void Resolve(object target, IReadOnlyList<SignalParamEntry> entries, object[] values)
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
                int claimedCount = 0;

                for (int c = 0; c < candidates.Count; c++)
                {
                    if (_claimedBy[candidates[c]] != null)
                    {
                        claimedCount++;
                        continue;
                    }

                    slot = candidates[c];
                    break;
                }

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
