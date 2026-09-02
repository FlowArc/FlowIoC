#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.Screens
{
    /// <summary>
    /// Which rows share a manager and a layer with another row. The runtime's IsLayerFull is keyed
    /// by that pair, so a screen opening on an occupied layer closes the one already there. That is
    /// legal and sometimes deliberate, so the panel marks it rather than refusing it.
    /// </summary>
    internal class ScreenLayerCollisions
    {
        internal HashSet<ScreenRowEVO> Find(IEnumerable<ScreenRowEVO> rows)
        {
            HashSet<ScreenRowEVO> collided = new HashSet<ScreenRowEVO>();

            if (rows == null)
                return collided;

            Dictionary<(int managerId, int layer), ScreenRowEVO> seen = new();

            foreach (ScreenRowEVO row in rows)
            {
                if (row?.Effective == null)
                    continue;

                (int, int) key = (row.Effective.ManagerId, row.Effective.Layer);

                if (seen.TryGetValue(key, out ScreenRowEVO first))
                {
                    collided.Add(first);
                    collided.Add(row);
                    continue;
                }

                seen[key] = row;
            }

            return collided;
        }
    }
}
#endif
