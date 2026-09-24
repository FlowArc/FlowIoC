#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// How wide a panel's list is: what the developer dragged it to, held between a floor that
    /// keeps a name readable and a ceiling that leaves the rows beside it room. The width is one
    /// developer's preference, so it lives in EditorPrefs - per panel, because a list of module
    /// names and a list of notification keys want different widths - and never in a committed
    /// asset.
    /// </summary>
    internal class ModulePanelSidebarWidth
    {
        internal const float DEFAULT = 200f;
        internal const float MIN = 160f;
        internal const float MAX = 480f;

        /// <summary>What the rows beside the list keep however far the list is dragged.</summary>
        internal const float ROWS_MIN = 320f;

        private const string PREFS_PREFIX = "FlowIoC.ModulePanel.SidebarWidth.";

        private readonly string _prefsKey;

        internal ModulePanelSidebarWidth(string panelTypeName)
        {
            _prefsKey = PREFS_PREFIX + panelTypeName;
            Value = EditorPrefs.GetFloat(_prefsKey, DEFAULT);
        }

        internal float Value { get; private set; }

        /// <summary>The width held inside the floor and the ceiling a window of this width allows.</summary>
        internal float Clamp(float width, float windowWidth) =>
            Mathf.Clamp(width, MIN, Mathf.Max(MIN, Mathf.Min(MAX, windowWidth - ROWS_MIN)));

        internal void Set(float width, float windowWidth) => Value = Clamp(width, windowWidth);

        internal void Save() => EditorPrefs.SetFloat(_prefsKey, Value);
    }
}

#endif
