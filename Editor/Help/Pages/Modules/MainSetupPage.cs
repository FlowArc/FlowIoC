#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>The module the game launches from, and MainScene with it.</summary>
    internal class MainSetupPage : ModulePage
    {
        public override string Title => "Main";

        public override string Subtitle => "Launches the game, owns MainScene";

        public override FlowIcon Icon => FlowIcon.Bolt;

        public override string ModuleFolderName => "MainModule";

        public override bool InSetupSet => true;

        public override string BodyHeadline => "The module the game starts from.";

        public override string BodyTagline =>
            "MainContext begins the Boot set and opens the main screen when it completes; MainScene "
            + "carries the package's own ScreenServiceRoot, PoolServiceRoot and AssetServiceRoot beside it.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it owns");
            painter.Bullet(
                "MainScene, the first scene in the build list, with the Roots of the whole set "
                + "instantiated in it.");
            painter.Bullet(
                "MainScreenModule under zScreenModules: the main screen, where Easy, Medium or Hard "
                + "is picked and carried to the gameplay screen as a signal parameter.");
            painter.Bullet(
                "MainContext: the Boot set is begun here, PreloadScreensCommand and FillPoolsCommand "
                + "report into it, and the main screen opens when the set completes.");

            painter.Note(
                "This module is the game's from the day it lands - rename it, gut it, rewrite the "
                + "flow. It carries no version and is never updated by the package.");
        }
    }
}

#endif
