#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Narrowing the console to one channel. Clicking channels off one at a time is what a reader
    /// does when a couple of them are noisy; soloing is what they want when only one of thirty is
    /// interesting, and clicking twenty-nine buttons is not an answer. Soloing the channel that is
    /// already alone puts every channel back, so the same gesture goes both ways.
    /// </summary>
    public class FlowConsoleSolo
    {
        public bool IsSoloed(IReadOnlyList<bool> visible, int index)
        {
            if (visible == null) return false;
            if (index < 0 || index >= visible.Count) return false;
            if (!visible[index]) return false;

            for (int i = 0; i < visible.Count; i++)
            {
                if (i != index && visible[i]) return false;
            }

            return true;
        }

        public void Apply(IList<bool> visible, int index)
        {
            if (visible == null) return;
            if (index < 0 || index >= visible.Count) return;

            bool restoreAll = IsSoloed(visible as IReadOnlyList<bool> ?? new List<bool>(visible), index);

            for (int i = 0; i < visible.Count; i++)
            {
                visible[i] = restoreAll || i == index;
            }
        }
    }
}
#endif
