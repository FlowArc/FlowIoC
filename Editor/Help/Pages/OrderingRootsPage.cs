#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

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

        public override FlowIcon Icon => FlowIcon.SortDown;

        /// <summary>
        /// The seats the shipped Roots already occupy. Exposed for the same reason the data type
        /// tree is: a test can check these against the initializeOrder actually serialised into
        /// the prefabs, so the page cannot quietly drift away from what the package ships.
        /// </summary>
        public IReadOnlyDictionary<string, int> Seats { get; } = new Dictionary<string, int>
        {
            {"AbTestFlowServiceRoot", -90},
            {"AssetServiceRoot", -80},
            {"ScreenServiceRoot", -70},
            {"PoolServiceRoot", -60},
            {"HapticServiceRoot", -50},
            {"WorldPointerServiceRoot", -30},
            {"LoadingServiceRoot", -10},
            {"GameplaySystemRoot", 0},
            {"CameraSystemRoot", 1},
            {"ScreenRoot", 99},
            {"MainRoot", 100},
            {"ConnectorRoot", 98}
        };

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Picking a number", DrawPicking,
                "Pick the band first, the number second.",
                "Where a new Root goes follows from what it is, and the number inside the band "
                + "rarely matters - two modules that never touch can both sit at 0.")
        };

        protected override string BodyHeadline => "Initialize Order is the only lever over what is built first.";

        protected override string BodyTagline =>
            "Every Root carries the number at the top of its inspector, and RootsManager sorts every "
            + "Root by it and drives them in that order. The number is not free-form: the Roots "
            + "FlowIoC ships fall into bands, and placing a new Root means picking its band.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("The bands");
            painter.Paragraph("The whole range is -100 to 100. Nothing needs to sit outside it.");
            painter.Table(new[] {"Order", "Who sits there"},
                new[]
                {
                    "-100 to -90",
                    "Services, on the tens, in the order the boot reads. -100 and -90 are the two that "
                    + "put data in place before anything reads it: saved data restored at -100, config "
                    + "rewritten from an A/B assignment at -90. PostConstruct runs during the binding "
                    + "pass, so these go first, and a service whose PostConstruct reads config sits "
                    + "after -90."
                },
                new[]
                {
                    "-80 to -10",
                    "-80 the asset service, the door every Addressables load goes through. -70 the "
                    + "screen service and -60 the pool service, which load through it. Then the "
                    + "helpers: -50 haptics, -30 world pointers, -10 the loading service - last of the "
                    + "services, because it opens a screen."
                },
                new[]
                {
                    "0 to 97",
                    "The game's own modules and Systems. Gameplay, camera, whatever this game is made of."
                },
                new[]
                {
                    "98",
                    "ConnectorRoot. After every module it wires, so the scene reads as modules first "
                    + "and wiring after them."
                },
                new[]
                {
                    "99",
                    "ScreenRoot. The screen manager owns the layers the screens open in, so it is up "
                    + "before the flow that opens the first screen."
                },
                new[] {"100", "MainRoot. The entry point. Its Launch dispatches the first signal, last of all."});

            painter.Space();
            painter.Note(
                "A game's own service takes a free ten, or a unit below the ten it leans on - -69 for "
                + "one that wants the screen service bound first. Inside the 0 to 97 band the exact "
                + "number rarely matters - two modules that never touch can both sit at 0.");

            painter.Separator();
            painter.SubHeading("The scene reads top to bottom");
            painter.Paragraph(
                "MainScene is authored in the same order, with a separator between the bands, so the "
                + "Hierarchy shows the boot order without opening a single inspector. Keep a new Root "
                + "in its band's place in that list; a Hierarchy that disagrees with the numbers is a "
                + "trap for the next reader.");

            painter.Image(_images.Get("MainSceneHierarchy.png"),
                "MainScene: the services in boot order, then the game's modules, then ConnectorRoot, ScreenRoot and MainRoot.");

            painter.Separator();
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
            painter.SubHeading("Where a new Root goes");
            painter.Bullet(
                "A Service - self-contained, not specific to this game - takes a free ten in the negative band, or a unit below the ten it leans on. After -90 if its PostConstruct reads config, and below anything that injects it at bind time.");
            painter.Bullet(
                "A module or System this game is made of takes something in 0 to 97. Use 0 unless another Root genuinely has to bind first.");
            painter.Bullet(
                "The Connector, the screen host and the entry point are taken: 98, 99, 100. A second Connector on a large project sits beside the first, still below ScreenRoot.");
            painter.Bullet("Then move the GameObject in the Hierarchy to where its number says it belongs.");

            painter.Separator();
            painter.SubHeading("What goes wrong");
            painter.Bullet(
                "A Root left at 0 that other Roots inject from. It binds in registration order relative to its peers, so the failure is intermittent - fine on one machine, null on another.");
            painter.Bullet(
                "A Connector mixed in among the modules it wires. It still works, because the barrier saves it, but the scene stops reading as modules first and wiring after them.");
            painter.Bullet(
                "Cross-module work done in Launch that belonged in Setup, then patched by nudging Initialize Order. The phase is the fix; the number is not.");
            painter.Bullet(
                "A Service given a positive number. It cannot need one - if it does it is a System, and it belongs in the 0 to 97 band with a name to match.");
            painter.Bullet(
                "A service whose PostConstruct reads config seated above -90. It reads the original before the A/B variant lands, and nothing is logged, because nothing went wrong.");

            painter.Space();
            painter.Note(
                "Each phase can also be switched off per Root - AutoInitialize, AutoBindInjections, "
                + "AutoBindMediations, AutoSetup, AutoLaunch - so a test scene can drive a context by "
                + "hand instead of reordering the scene around it.");
        }
    }
}

#endif
