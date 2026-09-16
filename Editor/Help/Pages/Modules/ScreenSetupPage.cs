#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>The ScreenManager and the layers every screen of the game opens into.</summary>
    internal class ScreenSetupPage : ModulePage
    {
        public override string Title => "Screen";

        public override string Subtitle => "The ScreenManager and its layers";

        public override FlowIcon Icon => FlowIcon.Layers;

        public override string ModuleFolderName => "ScreenModule";

        public override bool InSetupSet => true;

        public override string BodyHeadline => "Where every screen opens.";

        public override string BodyTagline =>
            "ScreenModule holds the ScreenManager and the layers every screen of the game opens into; "
            + "the screen service in the package drives it.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it owns");
            painter.Bullet(
                "The ScreenManager prefab under ScreenRoot, with the layer list a screen names when it "
                + "opens - Screen, Popup, Overlay - and the screen configs the setup screens register on.");
            painter.Bullet(
                "The ScreenServiceRoot in MainScene is the package's; this module is the game's half - "
                + "add a layer, reorder them, point a config at a screen of your own.");

            painter.Note(
                "This module is the game's from the day it lands. It carries no version and is never "
                + "updated by the package.");
        }
    }
}

#endif
