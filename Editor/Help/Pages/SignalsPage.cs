#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.Graph;

namespace FlowIoC.Editor.Help.Pages
{
    /// <summary>
    /// A module's public surface, read in four passes: the three kinds of signal as a picture,
    /// then the holder every module publishes, then the internal holder nobody outside may
    /// dispatch, then the rules.
    /// </summary>
    internal class SignalsPage : HelpPage
    {
        public SignalsPage() : base(Build())
        {
        }

        public override string Title => "Signals";

        public override string Icon => "Profiler.NetworkMessages";

        protected override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("The holder", DrawHolder),
            new HelpTab("Internal", DrawInternal),
            new HelpTab("Rules", DrawRules)
        };

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Hero(
                "A signal is a name and a payload.",
                "Together, Incoming and Outgoing are the module's whole public surface. Because the "
                + "surface is this narrow, a module can be rewritten from the inside without "
                + "anything else in the game noticing.");

            painter.Parts(
                new HelpPart("Incoming",
                    "What the module accepts. Each one is bound to a command in the Context, and "
                    + "anyone may dispatch it.",
                    "Signal<double> AddCurrency"),
                new HelpPart("Outgoing",
                    "What the module announces. Who listens is decided elsewhere, in a Connector or "
                    + "in a Mediator.",
                    "Signal<double> CurrencyChanged"),
                new HelpPart("Internal",
                    "What the module says to itself. It is internal, so nothing outside the "
                    + "module's own assembly can dispatch it.",
                    "Signal Tick"));

            painter.SubHeading("What crosses the boundary");
            painter.Paragraph(
                "Two arrows, and everything another module is allowed to know. The one other thing "
                + "it may reference directly is the interface of a Service.");

            painter.Space();
            painter.Graph(Graph, Stepper);

            painter.Space();
            painter.Note(
                "A public signal may only carry a type that lives in Shared too. A "
                + "Signal<CameraCVO> means CameraCVO belongs in Scripts/Shared/Data/ValueObjects.");
        }

        /// <summary>
        /// The public holder: what it looks like, where it lives, and the two ways a signal is
        /// answered.
        /// </summary>
        private void DrawHolder(HelpPainter painter)
        {
            painter.Hero(
                "One class holds them, and it lives in Shared.",
                "PlayerSignals compiles into Modules.Player.Shared, so a Connector can reach the "
                + "signals without gaining access to a single Model or Command.");

            painter.SubHeading("Writing one");
            painter.Code(
                "public class PlayerSignals : ISignalHolder\n"
                + "{\n"
                + "    public PlayerSignalsIncoming Incoming = new();\n"
                + "    public PlayerSignalsOutgoing Outgoing = new();\n"
                + "}\n"
                + "\n"
                + "public class PlayerSignalsIncoming\n"
                + "{\n"
                + "    public Signal InitializePlayer = new();\n"
                + "    public Signal<double> AddCurrency = new();\n"
                + "}\n"
                + "\n"
                + "public class PlayerSignalsOutgoing\n"
                + "{\n"
                + "    public Signal<double> CurrencyChanged = new();\n"
                + "}",
                "PlayerSignals.cs - Scripts/Shared/Signals");
            painter.Paragraph(
                "Signals come in five arities, from Signal to Signal<T1, T2, T3, T4>. The module "
                + "that owns a holder is the one that binds it, in SignalBindings.");
            painter.Code(
                "_signals = InjectionBinderCrossContext.Bind<PlayerSignals>();",
                "PlayerContext.cs - SignalBindings");

            painter.Space();
            painter.SubHeading("Answering one");
            painter.Paragraph(
                "A signal is answered by a command binding, by a direct listener, or by both. The "
                + "binding is what shows up in the Flow Console; a listener is what a Mediator uses "
                + "to write a new value into its View.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.AddCurrency)\n"
                + "    .ToSequence<AddCurrencyCommand>();");
            painter.Code(
                "_signals.Outgoing.CurrencyChanged.AddListener(OnCurrencyChanged);\n"
                + "_signals.Outgoing.CurrencyChanged.AddListenerOnce(OnFirstChangeOnly);\n"
                + "_signals.Outgoing.CurrencyChanged.RemoveListener(OnCurrencyChanged);\n"
                + "\n"
                + "_signals.Incoming.AddCurrency.Dispatch(100d);");

            painter.Space();
            painter.Note(
                "A Model may dispatch its own module's outgoing signals to announce that a value it "
                + "holds has changed. Announcing is allowed; listening is not.");
        }

        /// <summary>
        /// The internal holder, and the one thing that makes it a different kind of object: it
        /// crosses no boundary, so it has no two halves.
        /// </summary>
        private void DrawInternal(HelpPainter painter)
        {
            painter.Hero(
                "A signal that never leaves the module has no Incoming and no Outgoing.",
                "Those two halves describe a boundary. An internal signal never crosses one, so it "
                + "sits flat in a holder of its own.");

            painter.SubHeading("Writing one");
            painter.Code(
                "internal class PlayerInternalSignals : ISignalHolder\n"
                + "{\n"
                + "    public Signal Tick = new(hideCommandLog: true);\n"
                + "    public Signal<double> RecalculateInterest = new();\n"
                + "}",
                "PlayerInternalSignals.cs - Scripts/Runtime/Signals");
            painter.Paragraph(
                "The class is internal, so nothing outside Modules.Player can dispatch it or even "
                + "name it. A signal that fires every frame takes hideCommandLog so it does not bury "
                + "the Flow Console.");

            painter.Space();
            painter.SubHeading("Two holders, two folders");
            painter.Paragraph(
                "The folder a holder sits in is what decides which assembly it compiles into, and "
                + "that is the whole of the difference between the two.");
            painter.Code(
                "Scripts/\n"
                + "├── Runtime/\n"
                + "│   └── Signals/          PlayerInternalSignals - Modules.Player\n"
                + "└── Shared/\n"
                + "    └── Signals/          PlayerSignals         - Modules.Player.Shared");
            painter.Paragraph(
                "A Connector references Modules.Player.Shared and never Modules.Player, which is "
                + "what keeps one module's assembly out of another's.");
        }

        private void DrawRules(HelpPainter painter)
        {
            painter.Bullet("A signal is a name and a payload. Incoming is what the module accepts, Outgoing is what it announces.");
            painter.Bullet("The public holder lives in Scripts/Shared/Signals, so it compiles into the module's Shared assembly.");
            painter.Bullet("Whatever a public signal carries lives in Shared too, or the holder cannot name it.");
            painter.Bullet("The internal holder lives in Scripts/Runtime/Signals and has no Incoming and no Outgoing.");
            painter.Bullet("The module that owns a holder is the one that binds it. A Connector gets it, and never binds it.");
            painter.Bullet("A Model may dispatch its own module's outgoing signals. It never subscribes to any signal.");
        }

        private static HelpGraph Build()
        {
            var nodes = new List<HelpGraphNode>
            {
                new HelpGraphNode("incoming", "Incoming", "what the module accepts", 0, 0),
                new HelpGraphNode("module", "PlayerModule", "state, logic, presentation", 0, 1),
                new HelpGraphNode("outgoing", "Outgoing", "what the module announces", 0, 2)
            };

            var edges = new List<HelpGraphEdge>
            {
                new HelpGraphEdge("incoming", "module", "runs"),
                new HelpGraphEdge("module", "outgoing", "dispatches")
            };

            var steps = new List<HelpGraphStep>
            {
                new HelpGraphStep("incoming",
                    "Incoming is what the module accepts. Each one is bound to a command in the Context.",
                    "public class PlayerSignalsIncoming\n{\n    public Signal InitializePlayer = new();\n    public Signal<double> AddCurrency = new();\n}"),
                new HelpGraphStep("module",
                    "Inside, the module is free. Nothing outside it may name a Model, a Command or a View of its own.",
                    "CommandBinder.Bind(_signals.Incoming.AddCurrency)\n    .ToSequence<AddCurrencyCommand>();"),
                new HelpGraphStep("outgoing",
                    "Outgoing is what the module announces. Who listens is decided elsewhere, in a Connector.",
                    "public class PlayerSignalsOutgoing\n{\n    public Signal<double> CurrencyChanged = new();\n}")
            };

            return new HelpGraph(nodes, edges, steps, 1.4f);
        }
    }
}

#endif