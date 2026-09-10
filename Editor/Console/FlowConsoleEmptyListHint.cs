#if UNITY_EDITOR
using System.Globalization;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The one line the list shows in place of rows when there are none to show and there is a
    /// reason for it. An empty list looks exactly like a quiet game, and the two are told apart
    /// here: every channel switched off is the trap that says nothing on its own, and rows the
    /// filters hold back are the softer case, said with their count.
    /// </summary>
    public class FlowConsoleEmptyListHint
    {
        public string Text(bool anyChannelShown, int hiddenRows)
        {
            if (!anyChannelShown)
                return "Every channel is switched off. Switch some on under Filters, or pick Presets > Project defaults.";

            if (hiddenRows == 1) return "1 row hidden by filters.";
            if (hiddenRows > 1) return hiddenRows.ToString("N0", CultureInfo.InvariantCulture) + " rows hidden by filters.";

            return null;
        }
    }
}
#endif