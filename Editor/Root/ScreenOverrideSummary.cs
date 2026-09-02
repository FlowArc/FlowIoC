#if UNITY_EDITOR
using FlowIoC.BaseModule.Root;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// The short text a folded sub-context entry carries in its header. An entry that takes the
    /// screen's declaration as it comes says nothing, so the summary reads as a marker that this
    /// Root deviates from the context class rather than as noise on every row.
    /// </summary>
    internal class ScreenOverrideSummary
    {
        internal string For(SubContextData data)
        {
            if (!data.OverrideScreen)
                return string.Empty;

            return $"M{data.ScreenManagerId} L{data.ScreenLayer}";
        }
    }
}
#endif
