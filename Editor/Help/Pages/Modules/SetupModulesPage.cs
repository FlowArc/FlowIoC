#if UNITY_EDITOR

using FlowIoC.Editor.SetupModules;
using UnityEditor;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// What arrived in a new project, and why. The set installs itself, so the reader meets these
    /// six modules before they ever open this window - the page is here to explain what they are
    /// looking at, and to offer the set to a project that was skipped because it already had
    /// modules of its own.
    /// </summary>
    internal class SetupModulesPage : HelpPage
    {
        private readonly SetupModulesStartup _setup = new SetupModulesStartup();
        private readonly HelpAction _install;

        private bool _isInstalled;
        private double _checkedAt = double.NegativeInfinity;

        public SetupModulesPage() : base(null)
        {
            _install = new HelpAction(
                () => IsInstalled() ? "Installed" : "Install",
                () => !IsInstalled(),
                Install);
        }

        public override string Title => "Setup Modules";

        public override string Subtitle => "What a new FlowIoC project starts with";

        public override string Icon => "Prefab Icon";

        public override HelpAction Action => _install;

        /// <summary>
        /// Whether the set is in the project, answered from a cache that goes stale after a second.
        /// The underlying check walks every asmdef under Assets once per module of the set, and the
        /// banner asks twice per repaint - often enough that doing the walk each time would cost
        /// real frames. A second is far below noticing, and installing clears the cache outright.
        /// </summary>
        private bool IsInstalled()
        {
            if (EditorApplication.timeSinceStartup - _checkedAt < 1d)
                return _isInstalled;

            _isInstalled = _setup.IsInstalled();
            _checkedAt = EditorApplication.timeSinceStartup;

            return _isInstalled;
        }

        private void Install()
        {
            // Whatever happened, what the cache holds is now a guess about a project that has
            // changed underneath it.
            _checkedAt = double.NegativeInfinity;

            _setup.InstallNow();
        }

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A project with no modules of its own gets these six the first time the Editor "
                + "opens on it. There is no button to press and no dialog to answer: a game that "
                + "starts on FlowIoC starts on a flow that already runs, and can read how it is "
                + "wired rather than be told.");

            painter.Space();
            painter.SubHeading("What is here");
            painter.Paragraph(
                "MainModule launches the game and owns MainScene. ScreenModule holds the "
                + "ScreenManager and the layers every screen opens into. ConnectorModule is where "
                + "the modules meet - one sub-context wiring MainModule to the main screen, and the "
                + "main screen to the gameplay screen. GameplayModule is the game itself.");
            painter.Paragraph(
                "MainScreenModule and GameplayScreenModule sit inside their parents, under "
                + "zScreenModules. Together they make the flow: the game launches, the main screen "
                + "opens, picking Easy, Medium or Hard closes it and opens the gameplay screen with "
                + "the difficulty carried as a signal parameter.");

            painter.Space();
            painter.SubHeading("Installed once, and only once");
            painter.Paragraph(
                "The set is recorded in ProjectSettings/FlowIoCSetup.json, which belongs in source "
                + "control. Delete one of the modules and it stays deleted: the file says the "
                + "question has been asked and answered. A project that already had modules when "
                + "FlowIoC arrived was skipped for the same reason, and can take the set from the "
                + "button above.");

            painter.Note(
                "These modules are yours once they land. Rename them, gut them, delete what the "
                + "game does not need - nothing here is reinstalled or repaired behind your back.");
        }
    }
}

#endif
