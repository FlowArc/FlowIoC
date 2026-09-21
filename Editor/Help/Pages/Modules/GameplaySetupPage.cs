#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>The game itself: an empty System with its screen, ready to be written into.</summary>
    internal class GameplaySetupPage : ModulePage
    {
        public override string Title => "Gameplay Module";

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
                "GameplaySystemContext, which binds GameplaySignals and nothing else yet: both halves of the "
                + "holder are empty, waiting for what the game announces and accepts.");
            painter.Bullet(
                "DifficultyType in the module's Shared assembly - Easy, Medium, Hard. Shared is where "
                + "it lives because the main screen names it too, and a screen of another module may "
                + "read a neighbour's Shared data and never its Runtime.");
            painter.Bullet(
                "GameplayScreenModule under zScreenModules: the screen the game is played on, opened in "
                + "Layer_1 with the difficulty as its parameter, with its test scene beside it.");

            painter.SubHeading("How the difficulty arrives");
            painter.Paragraph(
                "The main screen announces DifficultySelected with a DifficultyType. MainConnectorSubContext "
                + "joins it to the gameplay screen's OpenGameplayScreen, whose Command reads the value as a "
                + "[SignalParam] and hands it to the screen through SetParameters - the screen finds it in "
                + "Data.Parameters. GameplayModule itself never sees the main screen: the crossing is the "
                + "Connector's, and the value crosses as data from Shared.");
            painter.Code(
                "internal class OpenGameplayScreenCommand : Command\n"
                + "{\n"
                + "    [Inject]      private IScreenService _screenService { get; set; }\n"
                + "    [SignalParam] private DifficultyType _difficulty    { get; set; }\n"
                + "    ...\n"
                + "}",
                "GameplayScreenModule - the parameter arrives through [SignalParam], never through Execute");

            painter.SubHeading("Writing the game here");
            painter.Bullet(
                "Rules and state go in a Model under Models; the work a signal runs goes in a Command "
                + "under Controllers, bound as a step in GameplaySystemContext; what the game shows goes in "
                + "Views and Mediators, or in a screen module of its own under zScreenModules.");
            painter.Bullet(
                "Logic that is this game's is a System under Systems; logic the next game could use "
                + "unchanged is a Service, and a Service another module needs gets a module of its own.");
            painter.Bullet(
                "What the game says to the rest of the project goes in GameplaySignals.Outgoing, what "
                + "it accepts in Incoming, and the Connector joins them to whoever cares. A module that "
                + "reaches into GameplayModule directly is the one thing the rules forbid.");
            painter.PageLink("Systems and Services", "Read: Systems and Services - which of the two");
            painter.PageLink("Controllers", "Read: Controllers - Commands and Functions");
            painter.PageLink("Model", "Read: Model - state and the rules that keep it valid");

            painter.SubHeading("In the scene");
            painter.Table(new[] {"Root", "Initialize Order", "Role"},
                new[]
                {
                    "GameplaySystemRoot", "0",
                    "System - read off the name. The game's own modules sit in the 0 to 97 band, after every "
                    + "service they inject and before the Connector that wires them; two systems that never "
                    + "touch can share a number."
                });
            painter.PageLink("Ordering Roots", "Read: Ordering Roots - the bands and where a new Root goes");

            painter.Note(
                "This module is the game's from the day it lands. It carries no version and is never "
                + "updated by the package.");
        }
    }
}

#endif