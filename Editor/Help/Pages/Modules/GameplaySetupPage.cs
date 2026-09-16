#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>The game itself, and the screen it is played on.</summary>
    internal class GameplaySetupPage : ModulePage
    {
        public override string Title => "Gameplay";

        public override string Subtitle => "The game itself";

        public override FlowIcon Icon => FlowIcon.Puzzle;

        public override string ModuleFolderName => "GameplayModule";

        public override bool InSetupSet => true;

        public override string BodyHeadline => "Where the game is written.";

        public override string BodyTagline =>
            "GameplayModule is the game; GameplayScreenModule under it is the screen the difficulty "
            + "is played on.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it owns");
            painter.Bullet(
                "GameplayContext and the signals the main screen reaches it through - the difficulty "
                + "arrives as a signal parameter, wired in ConnectorModule.");
            painter.Bullet(
                "GameplayScreenModule under zScreenModules: the screen the game is played on, with its "
                + "test scene beside it.");

            painter.Note(
                "This module is the game's from the day it lands. It carries no version and is never "
                + "updated by the package.");
        }
    }
}

#endif
