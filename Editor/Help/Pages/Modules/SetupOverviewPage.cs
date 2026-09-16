#if UNITY_EDITOR

using FlowIoC.Editor.Icons;
using FlowIoC.Editor.SetupModules;
using UnityEditor;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// What arrived in a new project, and why. The set installs itself, so the reader meets these
    /// nine modules before they ever open this window - the page is here to explain what they are
    /// looking at, and to offer the set to a project that was skipped because it already had
    /// modules of its own. Each module of the set has a page beside this one; this page is the set
    /// as a whole.
    /// </summary>
    internal class SetupOverviewPage : HelpPage
    {
        private readonly SetupModulesStartup _setup = new SetupModulesStartup();
        private readonly HelpAction _install;

        private bool _isInstalled;
        private double _checkedAt = double.NegativeInfinity;

        public SetupOverviewPage() : base(null)
        {
            _install = new HelpAction(
                () => IsInstalled() ? "Installed" : "Install All Setup",
                () => !IsInstalled(),
                Install);
        }

        public override string Title => "Overview";

        public override string Subtitle => "What a new FlowIoC project starts with";

        public override FlowIcon Icon => FlowIcon.Grid;

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

            new SetupSetInstallAction(_setup).Run();
        }

        protected override string BodyHeadline => "A new project starts on a flow that already runs.";

        protected override string BodyTagline =>
            "A project with no modules of its own gets these nine the first time the Editor opens on "
            + "it. There is no button to press and no dialog to answer, so the wiring is something "
            + "to read rather than something to be told.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Space();
            painter.SubHeading("What is here");
            painter.Paragraph(
                "MainModule launches the game and owns MainScene. ScreenModule holds the "
                + "ScreenManager and the layers every screen opens into. ConnectorModule is where "
                + "the modules meet - one sub-context wiring MainModule to the main screen, and the "
                + "main screen to the gameplay screen. GameplayModule is the game itself. Above them "
                + "MainScene carries the package's own ScreenServiceRoot, PoolServiceRoot and "
                + "AssetServiceRoot - the screens are addressable, and AssetServiceRoot is what loads them.");
            painter.Paragraph(
                "MainScreenModule and GameplayScreenModule sit inside their parents, under "
                + "zScreenModules. Together they make the flow: the game launches, the main screen "
                + "opens, picking Easy, Medium or Hard closes it and opens the gameplay screen with "
                + "the difficulty carried as a signal parameter.");
            painter.Paragraph(
                "LoadingModule owns the boot's bar: MainContext begins the Boot set, "
                + "PreloadScreensCommand and FillPoolsCommand report into it, and the main screen "
                + "opens when the set completes. LoadingScreenModule and LoadingOverlayScreenModule "
                + "under it are the two presentations, and LoadingConnectorSubContext in "
                + "ConnectorModule joins them to the service. The Loading page has the rest.");
            painter.Paragraph(
                "Each module has a page of its own beside this one, with its button: Installed, "
                + "Update to X for the one that carries a version, or Install for a module the game "
                + "removed and wants back.");

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

            painter.Space();
            painter.SubHeading("Updating a ready-made module");
            painter.Paragraph(
                "Every module under Modules~ carries a version, the Version: line on its card, and "
                + "a record of what shipped beside it. When the package ships a newer version the "
                + "module's page reads Update to X, its sidebar row reads UPDATE, and one console "
                + "line at startup says so. Of the set only Loading carries a version - it is a "
                + "service, and its improvements reach a game the same way; the rest are yours from "
                + "the day they land and say Update: never on their cards.");
            painter.Paragraph(
                "An update keeps what you changed. A file only the package changed is overwritten, "
                + "a file only you changed stays, and a file both of you changed is listed before "
                + "anything is written - keep yours, or take the package's, for all of them at once.");
            painter.Note(
                "A module installed before it kept a record lists every file that differs from the "
                + "shipped one as a conflict, because nobody can say who changed it. Commit before "
                + "updating, and read the list.");
        }
    }
}

#endif
