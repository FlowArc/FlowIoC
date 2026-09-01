#if UNITY_EDITOR

using System.Collections.Generic;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// Initialize Order, which is the one lever a scene has over which module is built first.
    /// The numbers the shipped scene uses are not free-form - they are bands - so the page shows
    /// the bands, the seats that are already taken, and what the order does and does not buy.
    /// </summary>
    internal class OrderingRootsPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public OrderingRootsPage() : base(null)
        {
        }

        public override string Title => "Ordering Roots";

        public override string Subtitle => "Which module is built first";

        public override string Icon => "AlphabeticalSorting";

        /// <summary>
        /// The seats the shipped Roots already occupy. Exposed for the same reason the data type
        /// tree is: a test can check these against the initializeOrder actually serialised into
        /// the prefabs, so the page cannot quietly drift away from what the package ships.
        /// </summary>
        public IReadOnlyDictionary<string, int> Seats { get; } = new Dictionary<string, int>
        {
            {"ScreenServiceRoot", -99},
            {"PoolServiceRoot", -2},
            {"GameplayRoot", 0},
            {"ScreenRoot", 99},
            {"MainRoot", 100},
            {"ConnectorRoot", 98}
        };

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Picking a number", DrawPicking)
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "A module joins the game by having its Root in the scene, and every Root carries one "
                + "number: Initialize Order, at the top of its inspector. RootsManager sorts every "
                + "Root by it and drives them in that order, so it is the only lever there is over "
                + "which module is built first.");
            painter.Paragraph(
                "The number is not free-form. The Roots FlowIoC ships fall into bands, and placing a "
                + "new Root means picking the band it belongs to.");

            painter.SubHeading("The bands");
            painter.Bullet("Negative - Services. A Service depends on nothing else, so it comes up first and is ready for everyone.");
            painter.Bullet("0 to 97 - the game's own modules and Systems. Gameplay, input, camera, whatever this game is made of.");
            painter.Bullet("98 - ConnectorRoot. After every module it wires, so the scene reads as modules first and wiring after them.");
            painter.Bullet("99 - ScreenRoot. The screen manager owns the screen prefabs, so it is up before the flow that opens the first screen.");
            painter.Bullet("100 - MainRoot. The entry point. Its Launch dispatches the first signal, last of all.");

            painter.Space();
            painter.Note(
                "The shipped Roots use -10000 for the asset service, -99 for the screen service, -2 "
                + "for the pool service, 0 for gameplay and input, 1 for the camera system. Inside a "
                + "band the exact number rarely matters - two modules that never touch can both sit at 0.");

            painter.SubHeading("The scene reads top to bottom");
            painter.Paragraph(
                "MainScene is authored in the same order, with a separator between the bands, so the "
                + "Hierarchy shows the boot order without opening a single inspector. Keep a new Root "
                + "in its band's place in that list; a Hierarchy that disagrees with the numbers is a "
                + "trap for the next reader.");

            painter.Image(_images.Get("MainSceneHierarchy.png"),
                "MainScene: the two services, then the game's modules, then ConnectorRoot, ScreenRoot and MainRoot.");

            painter.SubHeading("What the order actually buys");
            painter.Paragraph(
                "StartContexts runs three passes. First, sorted by Initialize Order, every Root runs "
                + "its binding phases. Then a frame passes. Then Setup on every Root, and finally "
                + "Launch on every Root, both in the same sorted order.");
            painter.Paragraph(
                "So the number decides who binds first, and who is called first inside the Setup and "
                + "Launch passes. It is not what makes reaching across modules safe: the frame "
                + "barrier already guarantees that every signal holder in the scene exists before any "
                + "Setup runs. That is why a Connector does its work in Setup and why Launch is where "
                + "the first signal is dispatched.");

            painter.Space();
            painter.Note(
                "ConnectorRoot at 98 is therefore about reading order, not correctness. It sits after "
                + "every module it wires and before the screen host and the entry point; any other "
                + "number in the band would work just as well, because the barrier is what makes it safe.");
        }

        private void DrawPicking(HelpPainter painter)
        {
            painter.Rule("Pick the band first, the number second.");

            painter.SubHeading("Where a new Root goes");
            painter.Bullet("A Service - self-contained, not specific to this game - takes a negative number, below anything that injects it.");
            painter.Bullet(
                "A module or System this game is made of takes something in 0 to 97. Use 0 unless another Root genuinely has to bind first.");
            painter.Bullet(
                "The Connector, the screen host and the entry point are taken: 98, 99, 100. A second Connector on a large project sits beside the first, still below ScreenRoot.");
            painter.Bullet("Then move the GameObject in the Hierarchy to where its number says it belongs.");

            painter.SubHeading("What goes wrong");
            painter.Bullet(
                "A Root left at 0 that other Roots inject from. It binds in registration order relative to its peers, so the failure is intermittent - fine on one machine, null on another.");
            painter.Bullet(
                "A Connector mixed in among the modules it wires. It still works, because the barrier saves it, but the scene stops reading as modules first and wiring after them.");
            painter.Bullet(
                "Cross-module work done in Launch that belonged in Setup, then patched by nudging Initialize Order. The phase is the fix; the number is not.");
            painter.Bullet(
                "A Service given a positive number. It cannot need one - if it does it is a System, and it belongs in the 0 to 98 band with a name to match.");

            painter.Space();
            painter.Note(
                "Each phase can also be switched off per Root - AutoInitialize, AutoBindInjections, "
                + "AutoBindMediations, AutoSetup, AutoLaunch - so a test scene can drive a context by "
                + "hand instead of reordering the scene around it.");
        }
    }
}

#endif