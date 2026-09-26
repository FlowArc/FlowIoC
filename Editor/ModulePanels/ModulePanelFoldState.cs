#if UNITY_EDITOR

using UnityEditor;

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// Which of a panel's folding sections are shut. A section is keyed by the panel's type and its
    /// heading, so two panels with an "Items" section fold apart. What a developer folded is their
    /// own view of the panel, so it lives in EditorPrefs and outlasts a domain reload and a restart,
    /// and never reaches a committed asset. A section nobody has touched is open.
    /// </summary>
    internal class ModulePanelFoldState
    {
        private const string PREFS_PREFIX = "FlowIoC.ModulePanel.Fold.";

        private readonly string _panelTypeName;

        internal ModulePanelFoldState(string panelTypeName)
        {
            _panelTypeName = panelTypeName;
        }

        internal bool IsOpen(string heading) => EditorPrefs.GetBool(Key(heading), true);

        internal void SetOpen(string heading, bool open)
        {
            // An open section is the default, so opening one forgets it rather than storing true.
            if (open)
                EditorPrefs.DeleteKey(Key(heading));
            else
                EditorPrefs.SetBool(Key(heading), false);
        }

        internal string Key(string heading) => PREFS_PREFIX + _panelTypeName + "." + heading;
    }
}

#endif
