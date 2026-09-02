#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;
using FlowIoC.Editor.Help.WhatsNew;

namespace FlowIoC.Editor.Help.Pages
{
    internal class WelcomePage : HelpPage
    {
        /// <summary>
        /// The reading the window lands on after the package has been updated, named here so
        /// that the startup notice can ask for it rather than for a tab number.
        /// </summary>
        internal const string WHATS_NEW_TAB = "What's New";

        private IReadOnlyList<WhatsNewVersionEVO> _releases;

        public WelcomePage() : base(Build())
        {
        }

        public override string Title => "Welcome";

        public override string Subtitle => "What is FlowIoC";

        public override string Icon => "console.infoicon";

        public override bool Featured => true;

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab(WHATS_NEW_TAB, DrawWhatsNew)
        };

        /// <summary>
        /// What changed, newest first, read out of the changelog the package ships. Every entry
        /// is one line: the detail is in CHANGELOG.md for whoever wants it, and a reader who has
        /// just updated wants the headlines.
        /// </summary>
        private void DrawWhatsNew(HelpPainter painter)
        {
            _releases ??= new WhatsNewSource().Releases();

            if (_releases.Count == 0)
            {
                painter.Note(
                    "The changelog that ships with the package could not be read, so there is "
                    + "nothing to show here.");

                return;
            }

            painter.Rule("What changed, newest first. The full entries are in the package's CHANGELOG.md.");

            foreach (WhatsNewVersionEVO release in _releases)
            {
                painter.Space();
                painter.SubHeading(release.Date.Length > 0
                    ? $"{release.Version}  -  {release.Date}"
                    : release.Version);

                foreach (WhatsNewGroupEVO group in release.Groups)
                {
                    painter.Rule(group.Title);

                    foreach (string line in group.Lines)
                        painter.Bullet(line);
                }
            }
        }

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Paragraph(
                "FlowIoC is a signal-driven inversion of control framework for Unity. A game is "
                + "split into modules that own their state, their logic and their presentation. "
                + "Modules never reference one another; they are wired together declaratively by "
                + "Connectors.");
            painter.Paragraph(
                "Nothing in C# enforces that. The compiler is happy to let one module reach into "
                + "another; what keeps a FlowIoC project honest is the convention these pages "
                + "describe - and the assembly definition each module carries, which makes the "
                + "boundary real.");

            painter.SubHeading("Why");
            painter.Bullet("Modules that do not know each other. A module's signals are its whole public surface.");
            painter.Bullet("Wiring you can read: bindings in one Context, crossings in one Connector.");
            painter.Bullet("The flow is visible. Contexts, injections, signals and commands are logged to the Flow Console.");
            painter.Bullet("Generators instead of boilerplate: Create Module, Create Command, Create Model, Create View.");
            painter.Bullet("Good for one developer, better for a team: two people can own two modules and never collide.");

            painter.SubHeading("What is in the box");
            painter.Bullet("Property injection, per context and across contexts.");
            painter.Bullet("Typed signals in five arities, with command bindings and direct listeners.");
            painter.Bullet("Command sequences and parallel groups, with retain and release for asynchronous work.");
            painter.Bullet("View mediation: a View holds the scene references, a Mediator drives it.");
            painter.Bullet("Functions - injectable methods you call directly and get an answer from.");
            painter.Bullet("Connectors, so two modules meet in one readable place.");
            painter.Bullet("Bundled modules for screens, pooling, addressable assets and the Flow Console.");

            painter.SubHeading("How a click becomes a state change");
            painter.Paragraph(
                "One module, and the round trip a click makes through it before it comes back out "
                + "as something another module can act on.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Bullet("A Root puts the module in the scene and starts its Context, which declares every binding.");
            painter.Bullet("A Mediator turns raw input into an incoming signal. A Connector can dispatch the same signal from another module.");
            painter.Bullet("The CommandBinder runs the commands bound to that signal, in sequence or in parallel.");
            painter.Bullet("Commands act on Models, Services and Systems, and call Functions when they need a value back.");
            painter.Bullet("What happened is announced as an outgoing signal - who listens is not the module's business.");
            painter.Bullet("Mediators hear it and update their Views; Connectors carry it to other modules.");

            painter.Space();
            painter.Note(
                "Work down the topics on the left. Each one walks a single diagram step by step and "
                + "shows the code behind every step.");
        }

        /// <summary>
        /// The round trip, drawn as a map rather than a walk: this page has no steps, so the
        /// painter draws the boxes without the Previous and Next controls.
        /// </summary>
        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("root", "Root & Context", "starts the module, binds it", 0, 0),
                new HelpGraphNode("incoming", "Incoming Signals", "what the module accepts", 0, 1),
                new HelpGraphNode("commands", "Commands", "one unit of work each", 0, 2),
                new HelpGraphNode("views", "Views & Mediators", "presentation only", 1, 0),
                new HelpGraphNode("outgoing", "Outgoing Signals", "what the module announces", 1, 1),
                new HelpGraphNode("state", "Models, Services, Systems", "state and work", 1, 2),
                new HelpGraphNode("connector", "Connector", "the only place modules meet", 2, 1)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("root", "incoming", "binds"),
                new HelpGraphEdge("views", "incoming", "dispatches"),
                new HelpGraphEdge("incoming", "commands", "runs"),
                new HelpGraphEdge("commands", "state", "acts on"),
                new HelpGraphEdge("state", "outgoing", "announces"),
                new HelpGraphEdge("outgoing", "views", "listens"),
                new HelpGraphEdge("outgoing", "connector", "crosses")
            };

            return new HelpGraph(nodes, edges, new List<HelpGraphStep>(), 1.4f);
        }
    }
}

#endif