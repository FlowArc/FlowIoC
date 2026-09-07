#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;

namespace FlowIoC.Editor.Console
{
    public struct CollapsedRow
    {
        public ConsoleLog Log;
        public int Count;
    }

    /// <summary>
    /// Folds rows that say the same thing from the same place onto one line with a count. The
    /// first occurrence is the one kept, so a row does not move down the list as its count grows -
    /// a list that reorders itself under the reader cannot be read.
    /// </summary>
    public class FlowConsoleCollapse
    {
        private readonly Dictionary<int, int> _indexByKey = new();

        public List<CollapsedRow> Fold(IReadOnlyList<ConsoleLog> logs)
        {
            var rows = new List<CollapsedRow>();
            if (logs == null) return rows;

            _indexByKey.Clear();

            for (int i = 0; i < logs.Count; i++)
            {
                ConsoleLog log = logs[i];

                if (_indexByKey.TryGetValue(log.CollapseKey, out int index))
                {
                    CollapsedRow row = rows[index];
                    row.Count++;
                    rows[index] = row;
                    continue;
                }

                _indexByKey[log.CollapseKey] = rows.Count;
                rows.Add(new CollapsedRow {Log = log, Count = 1});
            }

            return rows;
        }
    }
}
#endif
