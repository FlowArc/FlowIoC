#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER

using UnityEditor.Toolbars;

namespace FlowIoC.Editor.SceneSwitcher
{
    /// <summary>
    /// The only static surface of the scene switcher. Unity discovers main toolbar elements
    /// through a static attributed method, so this type holds the single dropdown instance
    /// that callback needs and does nothing else; the behaviour lives on
    /// <see cref="SceneSwitcherDropdown"/>.
    /// </summary>
    public static class SceneSwitcherToolbar
    {
        private static SceneSwitcherDropdown _dropdown;

        private static SceneSwitcherDropdown Dropdown => _dropdown ??= new SceneSwitcherDropdown();

        [MainToolbarElement(SceneSwitcherDropdown.ELEMENT_PATH,
            defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreateSceneSwitcherDropdown() => Dropdown.CreateElement();
    }
}

#endif
