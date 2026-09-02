#if UNITY_EDITOR

using UnityEditor;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Whether FlowIoC decorates inspectors at all. The catch-all editor reaches every component
    /// that has no editor of its own, so there has to be a way to send the Editor back to how it
    /// drew before - a project that wants its inspectors untouched turns this off and loses
    /// nothing else.
    /// </summary>
    internal class FlowInspectorSettings
    {
        private const string PrefKey = "FlowIoC.Inspector.Enabled";
        private const string MenuPath = "Tools/FlowIoC/Inspector Bar";

        public bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath, false, -1050)]
        private static void Toggle()
        {
            var settings = new FlowInspectorSettings();
            settings.Enabled = !settings.Enabled;

            Menu.SetChecked(MenuPath, settings.Enabled);
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, new FlowInspectorSettings().Enabled);

            return true;
        }
    }
}

#endif
