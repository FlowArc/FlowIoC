using System.Collections.Generic;
using Modules.LoadingModule.Shared.Data.ValueObjects;
using Modules.LoadingModule.Shared.Enums;

namespace Modules.LoadingModule.LoadingScreenModule.Models
{
    public class LoadingScreenModel : ILoadingScreenModel
    {
        private class Entry
        {
            public LoadingSetStatusRVO Latest;
            public bool Closed;
            public string FailedStep;
        }

        private readonly Dictionary<string, Entry> _entries = new();

        private Entry EntryOf(string set)
        {
            if (!_entries.TryGetValue(set, out Entry entry))
                _entries[set] = entry = new Entry();

            return entry;
        }

        public void Remember(LoadingSetStatusRVO status)
        {
            Entry entry = EntryOf(status.Set);
            entry.Latest = status;

            if (status.State == LoadingSetState.Running)
            {
                entry.Closed = false;
                entry.FailedStep = null;
            }
        }

        public void RememberClosed(string set) => EntryOf(set).Closed = true;

        public void RememberFailed(string set, string step) => EntryOf(set).FailedStep = step;

        public bool TryGetLatest(string set, out LoadingSetStatusRVO status)
        {
            status = _entries.TryGetValue(set, out Entry entry) ? entry.Latest : null;
            return status != null;
        }

        public bool IsClosed(string set) => _entries.TryGetValue(set, out Entry entry) && entry.Closed;

        public string FailedStepOf(string set) => _entries.TryGetValue(set, out Entry entry) ? entry.FailedStep : null;
    }
}
