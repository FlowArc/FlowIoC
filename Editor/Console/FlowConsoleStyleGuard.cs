#if UNITY_EDITOR
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Whether a cached style is the one the window built, or the placeholder it can come back as.
    ///
    /// A style copied from an editor style that was not ready is a copy of nothing: every setter
    /// after the copy is lost, and it reads back with no name, no size and black text. Cached
    /// behind a null check it is never built again - and a domain reload serialises the window's
    /// private fields and hands the placeholder straight back, so closing and reopening the window
    /// was the only way out. The name is the one thing every build writes, so a style that does not
    /// carry it is the placeholder and is built again on the next repaint.
    /// </summary>
    public class FlowConsoleStyleGuard
    {
        public bool IsBuilt(GUIStyle style, string name)
        {
            return style != null && style.name == name;
        }
    }
}
#endif
